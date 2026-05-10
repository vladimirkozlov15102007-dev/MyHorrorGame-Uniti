using UnityEngine;

namespace OldAmberFactory.Rendering
{
    /// <summary>
    /// Very light subtle camera shake used by explosions, arrow impacts, footstep jolts.
    /// Applies positional + rotational Perlin noise on top of the camera's base local transform.
    /// </summary>
    public class CameraShaker : MonoBehaviour
    {
        [SerializeField] private float _ambientAmount = 0.0015f;
        [SerializeField] private float _ambientFrequency = 0.6f;

        private Vector3 _basePos;
        private Quaternion _baseRot;
        private float _impulse;
        private float _impulseDecay = 3f;

        private void Awake()
        {
            _basePos = transform.localPosition;
            _baseRot = transform.localRotation;
        }

        public void ImpulseFromDamage(float magnitude) => _impulse = Mathf.Max(_impulse, magnitude);
        public void ImpulseFromExplosion(float magnitude) => _impulse = Mathf.Max(_impulse, magnitude * 1.8f);

        private void LateUpdate()
        {
            float t = Time.time * _ambientFrequency;
            Vector3 noise = new Vector3(
                Mathf.PerlinNoise(t, 0.1f) - 0.5f,
                Mathf.PerlinNoise(t, 0.6f) - 0.5f,
                Mathf.PerlinNoise(t, 1.3f) - 0.5f
            );
            float total = _ambientAmount + _impulse;
            transform.localPosition = _basePos + noise * total;
            transform.localRotation = _baseRot * Quaternion.Euler(noise * total * 25f);
            _impulse = Mathf.MoveTowards(_impulse, 0f, _impulseDecay * Time.deltaTime);
        }
    }
}
