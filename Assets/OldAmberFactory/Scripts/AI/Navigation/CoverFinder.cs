using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace OldAmberFactory.AI
{
    /// <summary>
    /// Finds cover, flank, and ambush points relative to the player.
    /// Uses NavMesh sampling + line-of-sight checks. Also probes pre-placed CoverPoint components.
    /// </summary>
    public class CoverFinder : MonoBehaviour
    {
        private static readonly List<CoverPoint> _static = new();
        internal static void Register(CoverPoint c) { if (!_static.Contains(c)) _static.Add(c); }
        internal static void Unregister(CoverPoint c) { _static.Remove(c); }

        [SerializeField] private float searchRadius = 14f;
        [SerializeField] private int sampleCount = 16;
        [SerializeField] private LayerMask occluders = ~0;

        public bool FindCover(Vector3 from, Vector3 playerPos, out Vector3 result)
        {
            // First try static cover points.
            CoverPoint best = null;
            float bestScore = float.MinValue;
            foreach (var cp in _static)
            {
                if (cp == null) continue;
                if (Vector3.Distance(cp.transform.position, from) > searchRadius * 2f) continue;
                if (!IsCovered(cp.transform.position, playerPos)) continue;
                float score = -Vector3.Distance(cp.transform.position, from);
                if (score > bestScore) { bestScore = score; best = cp; }
            }
            if (best != null) { result = best.transform.position; return true; }

            // Fallback: sample around the agent.
            for (int i = 0; i < sampleCount; i++)
            {
                Vector2 r = Random.insideUnitCircle * searchRadius;
                Vector3 sample = from + new Vector3(r.x, 0f, r.y);
                if (!NavMesh.SamplePosition(sample, out var hit, 2f, NavMesh.AllAreas)) continue;
                if (IsCovered(hit.position, playerPos))
                {
                    result = hit.position;
                    return true;
                }
            }
            result = default;
            return false;
        }

        public bool FindFlank(Vector3 agent, Vector3 playerPos, Vector3 playerForward, out Vector3 result)
        {
            Vector3 side = Vector3.Cross(playerForward, Vector3.up).normalized;
            for (int sign = -1; sign <= 1; sign += 2)
            {
                Vector3 target = playerPos + side * sign * searchRadius;
                if (NavMesh.SamplePosition(target, out var hit, 3f, NavMesh.AllAreas))
                { result = hit.position; return true; }
            }
            result = default;
            return false;
        }

        public bool FindAmbush(Vector3 playerPos, out Vector3 result)
        {
            foreach (var cp in _static)
            {
                if (cp == null || !cp.isAmbush) continue;
                if (Vector3.Distance(cp.transform.position, playerPos) < searchRadius * 0.5f) continue;
                if (!IsCovered(cp.transform.position, playerPos)) continue;
                result = cp.transform.position;
                return true;
            }
            result = default;
            return false;
        }

        bool IsCovered(Vector3 from, Vector3 playerPos)
        {
            Vector3 eye = from + Vector3.up * 1.5f;
            Vector3 target = playerPos + Vector3.up * 1.5f;
            return Physics.Raycast(eye, (target - eye).normalized,
                Vector3.Distance(eye, target), occluders, QueryTriggerInteraction.Ignore);
        }
    }

    /// <summary>
    /// Tag in the scene to mark a cover or ambush spot. Registers itself with CoverFinder.
    /// </summary>
    public class CoverPoint : MonoBehaviour
    {
        public bool isAmbush;
        void OnEnable()  { CoverFinder.Register(this); }
        void OnDisable() { CoverFinder.Unregister(this); }

        void OnDrawGizmos()
        {
            Gizmos.color = isAmbush ? Color.red : Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.4f);
        }
    }
}
