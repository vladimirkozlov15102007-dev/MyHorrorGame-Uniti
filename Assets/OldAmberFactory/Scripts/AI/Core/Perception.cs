using UnityEngine;

namespace OldAmberFactory.AI
{
    /// <summary>
    /// Sight + hearing perception with line-of-sight occlusion checks.
    /// Subscribes to EventBus.NoiseEvent for hearing.
    /// </summary>
    public class Perception : MonoBehaviour
    {
        [Header("Sight")]
        [SerializeField] private float sightRange = 22f;
        [SerializeField] private float sightFOV = 130f;
        [SerializeField] private LayerMask occluders = ~0;
        [SerializeField] private Transform eyes;

        [Header("Hearing")]
        [SerializeField] private float hearingRangeMultiplier = 1f;
        [SerializeField] private float memoryDuration = 8f;

        public Transform KnownPlayer { get; private set; }
        public Vector3 LastKnownPosition { get; private set; }
        public float LastSeenTime { get; private set; } = -999f;
        public float LastHeardTime { get; private set; } = -999f;
        public bool HasLineOfSight { get; private set; }

        public event System.Action<Transform> OnPlayerSpotted;
        public event System.Action<Vector3> OnNoiseHeard;

        void OnEnable()  { Core.EventBus.Subscribe<Core.EventBus.NoiseEvent>(HandleNoise); }
        void OnDisable() { Core.EventBus.Unsubscribe<Core.EventBus.NoiseEvent>(HandleNoise); }

        void HandleNoise(Core.EventBus.NoiseEvent n)
        {
            if (n.source == gameObject) return;
            float d = Vector3.Distance(transform.position, n.position);
            if (d > n.radius * hearingRangeMultiplier) return;
            LastHeardTime = Time.time;
            LastKnownPosition = n.position;
            OnNoiseHeard?.Invoke(n.position);
        }

        public void Tick(Transform player)
        {
            if (player == null) { HasLineOfSight = false; return; }
            Vector3 origin = eyes ? eyes.position : transform.position + Vector3.up * 1.6f;
            Vector3 to = player.position + Vector3.up * 1.3f - origin;
            float dist = to.magnitude;
            if (dist > sightRange) { HasLineOfSight = false; return; }
            if (Vector3.Angle(transform.forward, to) > sightFOV * 0.5f) { HasLineOfSight = false; return; }
            if (Physics.Raycast(origin, to.normalized, out var hit, dist, occluders, QueryTriggerInteraction.Ignore))
            {
                // Ensure the ray actually hits the player hierarchy.
                if (!hit.collider.transform.IsChildOf(player) && hit.collider.transform != player)
                {
                    HasLineOfSight = false;
                    return;
                }
            }
            HasLineOfSight = true;
            KnownPlayer = player;
            LastKnownPosition = player.position;
            LastSeenTime = Time.time;
            OnPlayerSpotted?.Invoke(player);
        }

        public bool HasRecentKnowledge()
        {
            return Time.time - Mathf.Max(LastSeenTime, LastHeardTime) < memoryDuration;
        }
    }
}
