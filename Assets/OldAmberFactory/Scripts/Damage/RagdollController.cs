using System.Collections.Generic;
using UnityEngine;

namespace OldAmberFactory.Damage
{
    /// <summary>
    /// Collects all rigidbodies on an armature and toggles between animated and physical state.
    /// On enable: all bones kinematic, animator active.
    /// On ragdoll: animator disabled, bones physical, collisions re-routed to EnemyRagdoll layer.
    /// </summary>
    public class RagdollController : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private Collider _mainCollider;
        [SerializeField] private Rigidbody _mainRigidbody;

        private readonly List<Rigidbody> _bones = new();
        private readonly List<Collider> _boneColliders = new();
        private bool _isRagdoll;

        public bool IsRagdoll => _isRagdoll;

        private void Awake()
        {
            GetComponentsInChildren(true, _bones);
            GetComponentsInChildren(true, _boneColliders);
            _bones.Remove(_mainRigidbody);
            _boneColliders.Remove(_mainCollider);
            SetRagdoll(false, Vector3.zero, Vector3.zero, 0f);
        }

        public void SetRagdoll(bool on, Vector3 hitPoint, Vector3 hitDir, float impulse)
        {
            _isRagdoll = on;

            if (_animator != null) _animator.enabled = !on;
            if (_mainCollider != null) _mainCollider.enabled = !on;
            if (_mainRigidbody != null) _mainRigidbody.isKinematic = true;

            for (int i = 0; i < _bones.Count; i++)
            {
                var rb = _bones[i];
                if (rb == null) continue;
                rb.isKinematic = !on;
                rb.useGravity = on;
                if (on) rb.linearVelocity = Vector3.zero;
            }
            for (int i = 0; i < _boneColliders.Count; i++)
            {
                var c = _boneColliders[i];
                if (c == null) continue;
                c.enabled = on;
            }

            if (on && impulse > 0f)
            {
                var closest = FindClosestBone(hitPoint);
                if (closest != null)
                    closest.AddForceAtPosition(hitDir * impulse, hitPoint, ForceMode.Impulse);
            }
        }

        private Rigidbody FindClosestBone(Vector3 p)
        {
            float best = float.MaxValue;
            Rigidbody bestRb = null;
            for (int i = 0; i < _bones.Count; i++)
            {
                var rb = _bones[i];
                if (rb == null) continue;
                float d = (rb.worldCenterOfMass - p).sqrMagnitude;
                if (d < best) { best = d; bestRb = rb; }
            }
            return bestRb;
        }
    }
}
