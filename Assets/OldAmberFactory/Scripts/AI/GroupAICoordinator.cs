using System.Collections.Generic;
using OldAmberFactory.AI.BehaviorTree;
using OldAmberFactory.Core;
using UnityEngine;

namespace OldAmberFactory.AI
{
    /// <summary>
    /// Single brain that coordinates all alive skeletons.
    /// Responsibilities:
    ///   1) Propagate last-known-player-position between agents (they "call out" to each other).
    ///   2) Assign tactical roles based on PlayerBehaviorAnalyzer output.
    ///   3) Keep a suppressor active, distribute flankers, maintain surround geometry.
    ///
    /// Registers itself into ServiceLocator so skeletons can find it without hard refs.
    /// </summary>
    public class GroupAICoordinator : MonoBehaviour
    {
        [SerializeField] private PlayerBehaviorAnalyzer _analyzer;
        [SerializeField] private float _updateInterval = 0.5f;

        private readonly List<SkeletonArcher> _agents = new();
        private float _nextUpdateAt;
        private Vector3 _sharedLastKnown;
        private float _sharedLastKnownAge = 999f;

        public int AliveCount => _agents.Count;
        public PlayerTactic CurrentTactic => _analyzer != null ? _analyzer.CurrentTactic : PlayerTactic.Neutral;

        private void Awake() => ServiceLocator.Register(this);
        private void OnDestroy() => ServiceLocator.Unregister<GroupAICoordinator>();

        public void RegisterAgent(SkeletonArcher a) { if (!_agents.Contains(a)) _agents.Add(a); }
        public void UnregisterAgent(SkeletonArcher a) { _agents.Remove(a); }

        /// <summary>
        /// Any agent that sees or loses the player calls this; it becomes the shared intel broadcast.
        /// </summary>
        public void ReportPlayer(Vector3 position)
        {
            _sharedLastKnown = position;
            _sharedLastKnownAge = 0f;
        }

        private void Update()
        {
            _sharedLastKnownAge += Time.deltaTime;

            if (Time.time < _nextUpdateAt) return;
            _nextUpdateAt = Time.time + _updateInterval;

            PropagateIntel();
            AssignRoles();
        }

        private void PropagateIntel()
        {
            if (_sharedLastKnownAge > 12f) return; // stale

            for (int i = 0; i < _agents.Count; i++)
            {
                var a = _agents[i];
                if (a == null || a.BB == null) continue;
                if (!a.BB.Has(BBKeys.LastKnownPos))
                    a.BB.Set(BBKeys.LastKnownPos, _sharedLastKnown);
            }
        }

        private void AssignRoles()
        {
            var tactic = CurrentTactic;
            if (_agents.Count == 0) return;

            // Sort by distance to the shared last-known to pick primary assaulter vs flankers.
            _agents.Sort((x, y) =>
            {
                float dx = (x.transform.position - _sharedLastKnown).sqrMagnitude;
                float dy = (y.transform.position - _sharedLastKnown).sqrMagnitude;
                return dx.CompareTo(dy);
            });

            for (int i = 0; i < _agents.Count; i++)
            {
                var role = RoleFor(tactic, i, _agents.Count);
                var agent = _agents[i];
                if (agent == null || agent.BB == null) continue;
                agent.BB.Set(BBKeys.AssignedRole, role);
                agent.BB.Set(BBKeys.FlankDir, (i % 2 == 0) ? 1 : -1);
                agent.BB.Set(BBKeys.SuppressTarget, _sharedLastKnown);
            }
        }

        /// <summary>
        /// The core of the adaptive AI. Maps a classified player tactic to skeleton role spread.
        /// </summary>
        private static GroupRole RoleFor(PlayerTactic tactic, int index, int total)
        {
            switch (tactic)
            {
                case PlayerTactic.CoverCamper:
                    // Player uses covers a lot -> mostly flankers.
                    if (index == 0) return GroupRole.Suppressor;
                    return (index % 2 == 0) ? GroupRole.FlankLeft : GroupRole.FlankRight;

                case PlayerTactic.Runner:
                    // Player always moving -> predict, assault, light flank.
                    return index < total / 2 ? GroupRole.Assault : GroupRole.FlankRight;

                case PlayerTactic.PositionLocked:
                    // Player sits in one spot -> ambushers + one suppressor.
                    if (index == 0) return GroupRole.Suppressor;
                    return GroupRole.Ambusher;

                case PlayerTactic.Aggressive:
                    // Player rushes -> hold distance, suppress.
                    return index == 0 ? GroupRole.Assault : GroupRole.Suppressor;

                case PlayerTactic.Fixed_Shooter:
                    // Player camps one shooting position -> multiple suppressors + flank.
                    if (index < 2) return GroupRole.Suppressor;
                    return (index % 2 == 0) ? GroupRole.FlankLeft : GroupRole.FlankRight;

                default:
                    // Balanced default: 1 assault, 1 suppressor, rest flanks.
                    if (index == 0) return GroupRole.Assault;
                    if (index == 1) return GroupRole.Suppressor;
                    return (index % 2 == 0) ? GroupRole.FlankLeft : GroupRole.FlankRight;
            }
        }
    }
}
