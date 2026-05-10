using OldAmberFactory.Audio;
using UnityEngine;

namespace OldAmberFactory.Throwable
{
    /// <summary>
    /// A rigidbody prop the player can pick up and throw. On impact, broadcasts a noise event
    /// so AI perception can investigate the sound (bottles, pipes, rebar, cans, stones).
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public class ThrowableItem : MonoBehaviour
    {
        [SerializeField] private float _impactNoiseRadius = 10f;
        [SerializeField] private float _minImpactSpeed = 2.5f;
        [SerializeField] private AudioSource _impactSfx;
        [SerializeField] private AudioClip[] _impactClips;
        [SerializeField] private float _impactCooldown = 0.3f;

        private Rigidbody _rb;
        private float _nextImpactAt;
        public Rigidbody Body => _rb;

        private void Awake() => _rb = GetComponent<Rigidbody>();

        public void SetHeld(bool held, Transform socket = null)
        {
            _rb.isKinematic = held;
            _rb.detectCollisions = !held;
            if (held && socket != null)
            {
                transform.SetParent(socket, false);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
            }
            else
            {
                transform.SetParent(null, true);
            }
        }

        public void Throw(Vector3 velocity)
        {
            SetHeld(false);
            _rb.linearVelocity = velocity;
            _rb.angularVelocity = Random.insideUnitSphere * 6f;
        }

        private void OnCollisionEnter(Collision c)
        {
            if (Time.time < _nextImpactAt) return;
            if (c.relativeVelocity.magnitude < _minImpactSpeed) return;
            _nextImpactAt = Time.time + _impactCooldown;

            NoiseEventBus.Broadcast(transform.position, _impactNoiseRadius,
                NoiseSource.ThrownObjectImpact, gameObject);

            if (_impactSfx != null && _impactClips != null && _impactClips.Length > 0)
            {
                _impactSfx.pitch = Random.Range(0.92f, 1.08f);
                _impactSfx.PlayOneShot(_impactClips[Random.Range(0, _impactClips.Length)]);
            }
        }
    }
}
