using UnityEngine;

namespace OldAmberFactory.Player
{
    /// <summary>
    /// Procedural weapon sway. Reads mouse look delta and player motion;
    /// displaces the weapon rig with a soft spring so the gun lags behind crosshair and hand.
    /// </summary>
    public class WeaponSway : MonoBehaviour
    {
        [SerializeField] private InputRouter _input;
        [SerializeField] private FPSController _controller;

        [Header("Sway")]
        [SerializeField] private float _lookAmount = 0.02f;
        [SerializeField] private float _moveAmount = 0.015f;
        [SerializeField] private float _maxOffset = 0.06f;
        [SerializeField] private float _smooth = 10f;

        [Header("Tilt")]
        [SerializeField] private float _lookTiltAmount = 3.5f;
        [SerializeField] private float _moveTiltAmount = 2f;

        private Vector3 _basePos;
        private Quaternion _baseRot;
        private Vector3 _currentPos;
        private Quaternion _currentRot;

        private void Awake()
        {
            _basePos    = transform.localPosition;
            _baseRot    = transform.localRotation;
            _currentPos = _basePos;
            _currentRot = _baseRot;
        }

        private void LateUpdate()
        {
            if (_input == null) return;

            Vector2 look = _input.Look;
            Vector2 move = _input.Move;
            float sprintMul = _controller != null && _controller.IsSprinting ? 1.6f : 1f;

            Vector3 posOffset = new Vector3(
                Mathf.Clamp(-look.x * _lookAmount - move.x * _moveAmount, -_maxOffset, _maxOffset),
                Mathf.Clamp(-look.y * _lookAmount - Mathf.Abs(move.y) * _moveAmount * 0.6f, -_maxOffset, _maxOffset),
                0f
            );

            Quaternion rotOffset = Quaternion.Euler(
                look.y * _lookTiltAmount,
                -look.x * _lookTiltAmount,
                -move.x * _moveTiltAmount * sprintMul
            );

            _currentPos = Vector3.Lerp(_currentPos, _basePos + posOffset, _smooth * Time.deltaTime);
            _currentRot = Quaternion.Slerp(_currentRot, _baseRot * rotOffset, _smooth * Time.deltaTime);

            transform.localPosition = _currentPos;
            transform.localRotation = _currentRot;
        }
    }
}
