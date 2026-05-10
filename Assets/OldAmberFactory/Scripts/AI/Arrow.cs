using OldAmberFactory.Damage;
using UnityEngine;

namespace OldAmberFactory.AI
{
    /// <summary>
    /// Physics-driven arrow. Shares a rigidbody with gravity; on first collider impact
    /// the arrow either penetrates (thin mesh / flesh flagged) or sticks to the surface.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public class Arrow : MonoBehaviour
    {
        [SerializeField] private float _damage = 25f;
        [SerializeField] private float _impulse = 5f;
        [SerializeField] private float _lifetime = 14f;
        [SerializeField] private bool _canPenetrate = true;
        [SerializeField] private float _penetrationCost = 12f;   // energy lost per penetration
        [SerializeField] private LayerMask _penetrableLayers;
        [SerializeField] private float _velocityAlignLerp = 18f;

        private Rigidbody _rb;
        private GameObject _instigator;
        private bool _stuck;
        private float _diesAt;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _diesAt = Time.time + _lifetime;
        }

        public void Launch(Vector3 velocity, GameObject instigator)
        {
            _instigator = instigator;
            _rb.linearVelocity = velocity;
        }

        private void Update()
        {
            if (_stuck) return;
            if (Time.time > _diesAt) Destroy(gameObject);

            // orient along velocity for realistic ballistic silhouette
            if (_rb.linearVelocity.sqrMagnitude > 0.5f)
            {
                Quaternion target = Quaternion.LookRotation(_rb.linearVelocity.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, _velocityAlignLerp * Time.deltaTime);
            }
        }

        private void OnCollisionEnter(Collision c)
        {
            if (_stuck) return;
            var contact = c.GetContact(0);
            Vector3 dir = _rb.linearVelocity.normalized;
            float speed = _rb.linearVelocity.magnitude;

            var hb = c.collider.GetComponent<HitBox>();
            if (hb != null)
            {
                float dmg = DamageTable.ForBodyPart(hb.Part);
                var info = new DamageInfo
                {
                    amount = dmg,
                    bodyPart = hb.Part,
                    hitPoint = contact.point,
                    hitDirection = dir,
                    hitNormal = contact.normal,
                    impulse = _impulse,
                    instigator = _instigator,
                    canPenetrateArmor = _canPenetrate
                };
                hb.Receive(info);

                // penetration: reduce speed, allow trigger collider to keep moving
                if (_canPenetrate && speed > _penetrationCost)
                {
                    _rb.linearVelocity = dir * (speed - _penetrationCost);
                    return;
                }
            }
            else if (_canPenetrate && IsPenetrable(c.collider.gameObject.layer) && speed > _penetrationCost)
            {
                _rb.linearVelocity = dir * (speed - _penetrationCost);
                return;
            }

            Stick(contact);
        }

        private bool IsPenetrable(int layer) => (_penetrableLayers.value & (1 << layer)) != 0;

        private void Stick(ContactPoint contact)
        {
            _stuck = true;
            _rb.isKinematic = true;
            transform.position = contact.point;
            transform.rotation = Quaternion.LookRotation(-contact.normal);
            transform.SetParent(contact.otherCollider.transform, true);
            Destroy(gameObject, 8f);
        }
    }
}
