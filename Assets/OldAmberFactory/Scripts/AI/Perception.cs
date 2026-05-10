using OldAmberFactory.Audio;
using UnityEngine;

namespace OldAmberFactory.AI
{
    /// <summary>
    /// Sight and hearing for a skeleton. Writes directly to its agent's blackboard:
    /// - sawPlayer, lastKnownPos, timeSinceSeen for vision,
    /// - heardNoisePos, timeSinceHeard for noise events.
    /// </summary>
    public class Perception : MonoBehaviour
    {
        [SerializeField] private Transform _head;
        [SerializeField] private float _sightRange = 22f;
        [SerializeField] private float _sightFovDeg = 120f;
        [SerializeField] private float _hearingRange = 18f;
        [SerializeField] private LayerMask _sightObstacles = ~0;
        [SerializeField] private Transform _playerTransform;

        private BehaviorTree.Blackboard _bb;
        private float _lastHeardAt = -999f;
        private float _lastSeenAt  = -999f;

        public void Bind(BehaviorTree.Blackboard bb) => _bb = bb;

        private void OnEnable()  { NoiseEventBus.OnNoise += OnNoise; }
        private void OnDisable() { NoiseEventBus.OnNoise -= OnNoise; }

        private void Update()
        {
            if (_bb == null) return;

            _bb.Set(BehaviorTree.BBKeys.TimeSinceSeen, Time.time - _lastSeenAt);
            _bb.Set(BehaviorTree.BBKeys.TimeSinceHeard, Time.time - _lastHeardAt);

            if (_playerTransform == null) return;

            Vector3 origin = _head != null ? _head.position : transform.position + Vector3.up * 1.6f;
            Vector3 toP = _playerTransform.position - origin;
            float dist = toP.magnitude;

            bool sees = false;
            if (dist <= _sightRange)
            {
                Vector3 fwd = _head != null ? _head.forward : transform.forward;
                float angle = Vector3.Angle(fwd, toP);
                if (angle <= _sightFovDeg * 0.5f)
                {
                    if (!Physics.Raycast(origin, toP.normalized, dist - 0.3f, _sightObstacles, QueryTriggerInteraction.Ignore))
                        sees = true;
                }
            }

            _bb.Set(BehaviorTree.BBKeys.SawPlayer, sees);
            if (sees)
            {
                _bb.Set(BehaviorTree.BBKeys.PlayerRef, _playerTransform);
                _bb.Set(BehaviorTree.BBKeys.LastKnownPos, _playerTransform.position);
                _lastSeenAt = Time.time;
            }
        }

        private void OnNoise(NoiseEvent e)
        {
            if (_bb == null) return;
            if (e.emitter == gameObject) return;

            float dist = Vector3.Distance(transform.position, e.position);
            if (dist > Mathf.Min(_hearingRange, e.radius)) return;

            _bb.Set(BehaviorTree.BBKeys.HeardNoisePos, e.position);
            _lastHeardAt = Time.time;
        }
    }
}
