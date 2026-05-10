using UnityEngine;

namespace OldAmberFactory.Environment
{
    /// <summary>
    /// Horror-style fluorescent flicker. Works on HDAdditionalLightData or plain Light.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class FlickerLight : MonoBehaviour
    {
        [SerializeField] private float minIntensity = 0f;
        [SerializeField] private float maxIntensity = 1.2f;
        [SerializeField] private float minInterval = 0.04f;
        [SerializeField] private float maxInterval = 0.35f;
        [SerializeField] private AnimationCurve burst = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private float brokenChance = 0.08f;
        [SerializeField] private AudioSource crackleSource;

        private Light _light;
        private float _nextChange;
        private float _baseIntensity;
        private bool _broken;

        void Awake()
        {
            _light = GetComponent<Light>();
            _baseIntensity = _light.intensity;
            _broken = Random.value < brokenChance;
        }

        void Update()
        {
            if (Time.time < _nextChange) return;
            _nextChange = Time.time + Random.Range(minInterval, maxInterval);

            if (_broken)
            {
                _light.enabled = false;
                return;
            }

            float v = burst.Evaluate(Random.value);
            _light.intensity = Mathf.Lerp(minIntensity, maxIntensity * _baseIntensity, v);
            _light.enabled = Random.value > 0.1f;

            if (crackleSource && Random.value < 0.15f) crackleSource.Play();
        }
    }
}
