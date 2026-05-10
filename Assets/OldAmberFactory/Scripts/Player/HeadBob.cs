using UnityEngine;

namespace OldAmberFactory.Player
{
    /// <summary>
    /// Subtle vertical / lateral camera bob while walking or sprinting.
    /// Frequency and amplitude scale with speed; fades while airborne or aiming down sights.
    /// </summary>
    public class HeadBob : MonoBehaviour
    {
        [SerializeField] private FPSController _controller;
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private float _walkFrequency = 9f;
        [SerializeField] private float _sprintFrequency = 13f;
        [SerializeField] private float _amplitude = 0.045f;
        [SerializeField] private float _lateralMultiplier = 0.55f;
        [SerializeField] private float _damping = 10f;

        private Vector3 _baseLocalPos;
        private float _cycle;

        private void Awake()
        {
            if (_cameraTransform != null)
                _baseLocalPos = _cameraTransform.localPosition;
        }

        private void LateUpdate()
        {
            if (_cameraTransform == null || _controller == null) return;

            Vector3 horiz = _controller.Velocity;
            horiz.y = 0f;
            float speed = horiz.magnitude;

            Vector3 target = _baseLocalPos;
            if (_controller.IsGrounded && speed > 0.2f)
            {
                float freq = _controller.IsSprinting ? _sprintFrequency : _walkFrequency;
                _cycle += Time.deltaTime * freq;
                float y = Mathf.Sin(_cycle) * _amplitude;
                float x = Mathf.Cos(_cycle * 0.5f) * _amplitude * _lateralMultiplier;
                target += new Vector3(x, y, 0f);
            }

            _cameraTransform.localPosition = Vector3.Lerp(
                _cameraTransform.localPosition, target, _damping * Time.deltaTime);
        }
    }
}
