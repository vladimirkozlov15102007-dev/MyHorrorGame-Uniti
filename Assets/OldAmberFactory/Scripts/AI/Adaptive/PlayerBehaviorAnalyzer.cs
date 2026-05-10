using System.Collections.Generic;
using UnityEngine;

namespace OldAmberFactory.AI
{
    /// <summary>
    /// Observes the player's behavior and exposes a normalized "style" profile that
    /// enemies use to adapt their tactics.
    /// </summary>
    public class PlayerBehaviorAnalyzer : MonoBehaviour
    {
        public static PlayerBehaviorAnalyzer Instance { get; private set; }

        [SerializeField] private Transform player;
        [SerializeField] private float sampleInterval = 0.5f;
        [SerializeField] private float historyWindow = 45f;

        private class Sample
        {
            public float time;
            public Vector3 position;
            public bool crouching;
            public bool running;
            public bool firing;
            public bool nearCover;
        }

        private readonly Queue<Sample> _samples = new();

        // Style scores (0..1):
        public float CoverUsage { get; private set; }      // hides a lot
        public float Aggression { get; private set; }      // fires a lot, pushes forward
        public float Mobility { get; private set; }        // keeps running
        public float Campiness { get; private set; }       // stays in one spot
        public float Predictability { get; private set; }  // repeats routes

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            InvokeRepeating(nameof(Sample_), 1f, sampleInterval);
        }

        public void SetPlayer(Transform p) { player = p; }
        public void NotifyFiring() { if (_samples.Count > 0) _samples.Peek().firing = true; }

        void Sample_()
        {
            if (player == null) return;
            var pc = player.GetComponent<OldAmberFactory.Player.PlayerController>();
            var s = new Sample
            {
                time = Time.time,
                position = player.position,
                crouching = pc && pc.IsCrouching,
                running = pc && pc.IsRunning,
                nearCover = CheckNearCover(player.position)
            };
            _samples.Enqueue(s);
            while (_samples.Count > 0 && Time.time - _samples.Peek().time > historyWindow)
                _samples.Dequeue();
            Recompute();
        }

        bool CheckNearCover(Vector3 pos)
        {
            // Heuristic: environment within 1.2m in 3 directions counts as cover.
            int hits = 0;
            foreach (var dir in new[] { Vector3.forward, Vector3.right, -Vector3.forward, -Vector3.right })
                if (Physics.Raycast(pos + Vector3.up * 1.2f, dir, 1.2f, ~0, QueryTriggerInteraction.Ignore))
                    hits++;
            return hits >= 2;
        }

        void Recompute()
        {
            if (_samples.Count < 3) return;
            int cover = 0, run = 0, crouch = 0, fire = 0;
            float totalDist = 0f;
            Vector3? prev = null;
            Vector3 sum = Vector3.zero;
            foreach (var s in _samples)
            {
                if (s.nearCover) cover++;
                if (s.running) run++;
                if (s.crouching) crouch++;
                if (s.firing) fire++;
                if (prev.HasValue) totalDist += Vector3.Distance(prev.Value, s.position);
                prev = s.position;
                sum += s.position;
            }
            int n = _samples.Count;
            Vector3 centroid = sum / n;
            float variance = 0f;
            foreach (var s in _samples) variance += (s.position - centroid).sqrMagnitude;
            variance /= n;

            CoverUsage     = Mathf.Clamp01((float)cover / n);
            Mobility       = Mathf.Clamp01(totalDist / (n * 3f));
            Aggression     = Mathf.Clamp01((float)fire / n * 2f);
            Campiness      = Mathf.Clamp01(1f - variance / 25f);
            Predictability = Mathf.Clamp01(1f - (variance / 80f));
        }

        /// <summary>
        /// Picks a tactic name based on style. Used by TacticsController.
        /// </summary>
        public string RecommendCounterTactic()
        {
            // Priority rules per spec.
            if (CoverUsage > 0.5f) return "flank";            // player hides -> flank
            if (Mobility > 0.55f)  return "intercept";        // predict route
            if (Campiness > 0.55f) return "ambush";           // set ambush
            if (Aggression > 0.55f) return "keep_distance";   // hold distance
            if (Predictability > 0.6f) return "suppress";     // suppressive fire
            return "default";
        }
    }
}
