using System.Collections.Generic;
using UnityEngine;

namespace OldAmberFactory.AI
{
    /// <summary>
    /// Shares information between skeleton members and assigns tactical roles:
    /// - attacker: direct assault
    /// - flanker: flanks
    /// - suppressor: fires from range to pin player
    /// - ambusher: sets up in an ambush point
    /// </summary>
    public class SquadCoordinator : MonoBehaviour
    {
        public static SquadCoordinator Instance { get; private set; }

        [SerializeField] private float shareRadius = 35f;
        [SerializeField] private float reroleInterval = 5f;

        private readonly List<SkeletonAgent> _members = new();
        public Vector3 SharedPlayerPosition { get; private set; }
        public float SharedPlayerTime { get; private set; } = -999f;
        public bool HasSharedTarget => Time.time - SharedPlayerTime < 10f;

        void Awake()
        {
            if (Instance != null && Instance != this) Destroy(this); else Instance = this;
            InvokeRepeating(nameof(AssignRoles), 2f, reroleInterval);
        }

        public void Register(SkeletonAgent a) { if (!_members.Contains(a)) _members.Add(a); }
        public void Unregister(SkeletonAgent a) { _members.Remove(a); }

        public void ReportContact(SkeletonAgent reporter, Vector3 playerPos)
        {
            SharedPlayerPosition = playerPos;
            SharedPlayerTime = Time.time;
            foreach (var m in _members)
            {
                if (m == reporter || m == null) continue;
                if (Vector3.Distance(m.transform.position, reporter.transform.position) > shareRadius) continue;
                m.Blackboard.Set("lastKnownPos", playerPos);
                m.Blackboard.Set("lastSeenTime", Time.time);
                m.Blackboard.Set("sharedContact", true);
            }
        }

        public void AssignRoles()
        {
            if (!HasSharedTarget || _members.Count == 0) return;

            // Sort by distance to player: closest -> attacker, farthest -> suppressor, mid -> flanker.
            _members.Sort((a, b) =>
                Vector3.SqrMagnitude(a.transform.position - SharedPlayerPosition)
                .CompareTo(Vector3.SqrMagnitude(b.transform.position - SharedPlayerPosition)));

            for (int i = 0; i < _members.Count; i++)
            {
                var a = _members[i];
                if (a == null || a.IsDead) continue;

                string role;
                if (_members.Count == 1) role = "attacker";
                else if (i == _members.Count - 1) role = "suppressor";
                else if (i % 2 == 0) role = "flanker";
                else role = "attacker";

                // Honor any per-agent "wantAmbush" flag from tactic hints.
                if (a.Blackboard.Get("wantAmbush", false)) role = "ambusher";
                if (a.Blackboard.Get("wantSuppress", false) && role == "attacker") role = "suppressor";

                a.Blackboard.Set("role", role);
            }
        }

        public int CountAlive()
        {
            int n = 0;
            foreach (var m in _members) if (m != null && !m.IsDead) n++;
            return n;
        }
    }
}
