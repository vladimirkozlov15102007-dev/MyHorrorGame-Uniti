using OldAmberFactory.Audio;
using UnityEngine;

namespace OldAmberFactory.Player
{
    /// <summary>
    /// First-person character controller driving a CharacterController.
    /// Handles walk/sprint/crouch/jump, stamina, ground check, look rotation,
    /// and broadcasts noise events to the world (for AI hearing).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FPSController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Transform _cameraPivot;
        [SerializeField] private InputRouter _input;
        [SerializeField] private PlayerStamina _stamina;

        [Header("Movement")]
        [SerializeField] private float _walkSpeed = 3.5f;
        [SerializeField] private float _sprintSpeed = 6.2f;
        [SerializeField] private float _crouchSpeed = 1.7f;
        [SerializeField] private float _airControl = 0.35f;
        [SerializeField] private float _acceleration = 14f;
        [SerializeField] private float _deceleration = 18f;
        [SerializeField] private float _jumpHeight = 1.15f;
        [SerializeField] private float _gravity = -22f;

        [Header("Look")]
        [SerializeField] private float _mouseSensitivity = 0.08f;
        [SerializeField] private float _maxPitch = 85f;

        [Header("Crouch")]
        [SerializeField] private float _standHeight = 1.8f;
        [SerializeField] private float _crouchHeight = 1.1f;
        [SerializeField] private float _crouchTransition = 8f;

        [Header("Noise (AI hearing)")]
        [SerializeField] private float _walkNoiseRadius = 4f;
        [SerializeField] private float _sprintNoiseRadius = 12f;
        [SerializeField] private float _crouchNoiseRadius = 1.5f;
        [SerializeField] private float _noiseInterval = 0.35f;

        private CharacterController _cc;
        private Vector3 _velocity;
        private Vector2 _horizontalVel;
        private float _yaw, _pitch;
        private float _nextNoiseTime;

        public bool IsCrouched { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool IsGrounded  => _cc != null && _cc.isGrounded;
        public Vector3 Velocity => _cc != null ? _cc.velocity : Vector3.zero;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            if (_input == null) return;

            HandleLook();
            HandleCrouchToggle();
            HandleMovement();
            EmitNoiseIfMoving();
        }

        private void HandleLook()
        {
            Vector2 d = _input.Look * _mouseSensitivity;
            _yaw   += d.x;
            _pitch  = Mathf.Clamp(_pitch - d.y, -_maxPitch, _maxPitch);

            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            if (_cameraPivot != null)
                _cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void HandleCrouchToggle()
        {
            if (_input.CrouchPressed)
                IsCrouched = !IsCrouched;

            float target = IsCrouched ? _crouchHeight : _standHeight;
            _cc.height = Mathf.MoveTowards(_cc.height, target, _crouchTransition * Time.deltaTime);
            _cc.center = new Vector3(0f, _cc.height * 0.5f, 0f);
        }

        private void HandleMovement()
        {
            Vector2 input = _input.Move;
            bool wantsSprint = _input.Sprint && !IsCrouched && input.y > 0.1f
                               && _stamina != null && _stamina.HasStamina;
            IsSprinting = wantsSprint;
            if (IsSprinting) _stamina?.Drain(Time.deltaTime);
            else              _stamina?.Regen(Time.deltaTime);

            float targetSpeed = IsCrouched  ? _crouchSpeed
                              : IsSprinting ? _sprintSpeed
                                            : _walkSpeed;

            Vector3 wish = transform.right * input.x + transform.forward * input.y;
            if (wish.sqrMagnitude > 1f) wish.Normalize();
            Vector3 wishVel = wish * targetSpeed;

            float accel = IsGrounded
                ? (wish.sqrMagnitude > 0.01f ? _acceleration : _deceleration)
                : _acceleration * _airControl;

            Vector3 horiz = new Vector3(_velocity.x, 0f, _velocity.z);
            horiz = Vector3.MoveTowards(horiz, wishVel, accel * Time.deltaTime);

            if (IsGrounded && _velocity.y < 0f) _velocity.y = -2f;
            if (_input.JumpPressed && IsGrounded && !IsCrouched
                && _stamina != null && _stamina.TrySpend(15f))
            {
                _velocity.y = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
            }
            _velocity.y += _gravity * Time.deltaTime;

            _velocity = new Vector3(horiz.x, _velocity.y, horiz.z);
            _cc.Move(_velocity * Time.deltaTime);
        }

        private void EmitNoiseIfMoving()
        {
            if (Time.time < _nextNoiseTime) return;
            if (Velocity.sqrMagnitude < 0.5f) return;
            if (!IsGrounded) return;

            float r = IsSprinting ? _sprintNoiseRadius
                    : IsCrouched  ? _crouchNoiseRadius
                                  : _walkNoiseRadius;

            _nextNoiseTime = Time.time + _noiseInterval;
            NoiseEventBus.Broadcast(transform.position, r, NoiseSource.Footstep, gameObject);
        }
    }
}
