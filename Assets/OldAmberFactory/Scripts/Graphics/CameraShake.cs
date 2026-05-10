using UnityEngine;

namespace OldAmberFactory.Graphics
{
    /// <summary>
    /// Lightweight camera shake. Call Kick(amount) on hits, explosions, footsteps of nearby heavy events.
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float damping = 6f;
        [SerializeField] private float maxTrauma = 1f;

        private float _trauma;
        private Vector3 _originalLocalPos;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            if (cameraTransform) _originalLocalPos = cameraTransform.localPosition;
        }

        public void Kick(float amount) { _trauma = Mathf.Min(maxTrauma, _trauma + amount); }

        void LateUpdate()
        {
            if (cameraTransform == null) return;
            float t = _trauma * _trauma;
            float x = (Mathf.PerlinNoise(Time.time * 23f, 0f) * 2f - 1f) * t * 0.08f;
            float y = (Mathf.PerlinNoise(0f, Time.time * 19f) * 2f - 1f) * t * 0.08f;
            float rz = (Mathf.PerlinNoise(Time.time * 15f, 10f) * 2f - 1f) * t * 1.2f;
            cameraTransform.localPosition = _originalLocalPos + new Vector3(x, y, 0f);
            cameraTransform.localRotation = Quaternion.Euler(0f, 0f, rz);
            _trauma = Mathf.Max(0f, _trauma - Time.deltaTime * damping);
        }
    }
}
