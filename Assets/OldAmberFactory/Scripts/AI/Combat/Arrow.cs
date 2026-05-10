using UnityEngine;
using OldAmberFactory.Damage;

namespace OldAmberFactory.AI
{
    /// <summary>
    /// Physics-based arrow. Uses rigidbody + orient-to-velocity. Sticks on impact.
    /// Pierces wood/cloth layers; stops on concrete/metal.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Arrow : MonoBehaviour
    {
        [SerializeField] private float damage = 14f;
        [SerializeField] private float life = 12f;
        [SerializeField] private float pierceChance = 0.25f;
        [SerializeField] private LayerMask hitMask = ~0;
        [SerializeField] private TrailRenderer trail;

        private Rigidbody _rb;
        private bool _stuck;
        private GameObject _shooter;

        void Awake() { _rb = GetComponent<Rigidbody>(); }

        public void Fire(Vector3 velocity, GameObject shooter)
        {
            _shooter = shooter;
            _rb.linearVelocity = velocity;
            Destroy(gameObject, life);
        }

        void FixedUpdate()
        {
            if (_stuck) return;
            if (_rb.linearVelocity.sqrMagnitude > 0.5f)
                transform.rotation = Quaternion.LookRotation(_rb.linearVelocity);
        }

        void OnCollisionEnter(Collision c)
        {
            if (_stuck) return;
            if (c.gameObject == _shooter) return;

            var hitbox = c.collider.GetComponent<Hitbox>() ?? c.collider.GetComponentInParent<Hitbox>();
            var contact = c.GetContact(0);
            if (hitbox != null)
            {
                hitbox.Receive(new DamageInfo
                {
                    baseDamage = damage,
                    hitPoint = contact.point,
                    hitDirection = _rb.linearVelocity.normalized,
                    hitForce = _rb.linearVelocity * _rb.mass,
                    source = _shooter,
                    isProjectile = true
                });
                Destroy(gameObject);
                return;
            }

            // Stick on the surface.
            _stuck = true;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic = true;
            if (trail) trail.emitting = false;
            transform.SetParent(c.collider.transform, worldPositionStays: true);
        }
    }
}
