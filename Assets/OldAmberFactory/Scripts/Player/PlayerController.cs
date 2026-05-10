using UnityEngine;
using OldAmberFactory.Damage;

namespace OldAmberFactory.Player
{
    /// <summary>
    /// First-person character controller with walking, running, jumping, crouching, stamina.
    /// Uses Unity CharacterController for stable collision response.
    /// Input is read in a simple way (Input.GetAxis) so it works without binding a full InputActions asset.
    /// In production, rebind to new InputSystem. Bindings:
    ///   WASD - move, Shift - run, Space - jump, Ctrl - crouch
    ///   E - interact (in Interactor), LMB/RMB/R - handled by WeaponController.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Health))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 3.2f;
        [SerializeField] private float runSpeed = 6.0f;
        [SerializeField] private float crouchSpeed = 1.6f;
        [SerializeField] private float jumpHeight = 1.1f;
        [SerializeField] private float gravity = -18f;
        [SerializeField] private float accel = 16f;

        [Header("Crouch")]
        [SerializeField] private float standHeight = 1.8f;
        [SerializeField] private float crouchHeight = 1.1f;
        [SerializeField] private float crouchTransition = 10f;

        [Header("Look")]
        [SerializeField] private Transform cameraRig;
        [SerializeField] private float mouseSensitivity = 1.8f;
        [SerializeField] private float pitchMin = -85f;
        [SerializeField] private float pitchMax = 85f;

        [Header("Stamina")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float staminaDrain = 22f;
        [SerializeField] private float staminaRegen = 14f;
        [SerializeField] private float staminaRegenDelay = 1.2f;

        [Header("Noise (used by AI perception)")]
        [SerializeField] private float walkNoise = 2.5f;
        [SerializeField] private float runNoise = 8.0f;
        [SerializeField] private float crouchNoise = 0.5f;
        [SerializeField] private float noiseBroadcastInterval = 0.4f;

        private CharacterController _cc;
        private Health _health;
        private Vector3 _velocity;
        private float _pitch;
        private float _stamina;
        private float _staminaCooldown;
        private float _noiseTimer;
        private bool _isCrouching;
        private bool _isRunning;

        public float Stamina => _stamina;
        public float StaminaNormalized => Mathf.Clamp01(_stamina / maxStamina);
        public bool IsCrouching => _isCrouching;
        public bool IsRunning => _isRunning;
        public Health Health => _health;
        public Transform CameraRig => cameraRig;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _health = GetComponent<Health>();
            _stamina = maxStamina;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void OnEnable()
        {
            _health.OnDied += HandleDied;
        }

        void OnDisable()
        {
            _health.OnDied -= HandleDied;
        }

        void HandleDied(DamageInfo _)
        {
            enabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Core.GameManager.Instance?.TriggerGameOver();
        }

        void Update()
        {
            HandleLook();
            HandleMove();
            HandleCrouch();
            HandleNoise();
        }

        private void HandleLook()
        {
            if (cameraRig == null) return;
            float mx = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
            float my = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
            transform.Rotate(0f, mx, 0f);
            _pitch = Mathf.Clamp(_pitch - my, pitchMin, pitchMax);
            cameraRig.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void HandleMove()
        {
            Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            if (input.sqrMagnitude > 1f) input.Normalize();

            bool wantsRun = Input.GetKey(KeyCode.LeftShift) && input.sqrMagnitude > 0.01f && !_isCrouching && _stamina > 0.5f;
            _isRunning = wantsRun;

            float target = _isCrouching ? crouchSpeed : (wantsRun ? runSpeed : walkSpeed);
            Vector3 desired = transform.TransformDirection(input) * target;

            Vector3 horizontal = new Vector3(_velocity.x, 0f, _velocity.z);
            horizontal = Vector3.MoveTowards(horizontal, desired, accel * Time.deltaTime);
            _velocity.x = horizontal.x;
            _velocity.z = horizontal.z;

            if (_cc.isGrounded)
            {
                if (_velocity.y < 0f) _velocity.y = -2f;
                if (Input.GetKeyDown(KeyCode.Space) && !_isCrouching)
                    _velocity.y = Mathf.Sqrt(-2f * gravity * jumpHeight);
            }
            _velocity.y += gravity * Time.deltaTime;
            _cc.Move(_velocity * Time.deltaTime);

            // Stamina
            if (wantsRun)
            {
                _stamina = Mathf.Max(0f, _stamina - staminaDrain * Time.deltaTime);
                _staminaCooldown = staminaRegenDelay;
            }
            else
            {
                _staminaCooldown = Mathf.Max(0f, _staminaCooldown - Time.deltaTime);
                if (_staminaCooldown <= 0f)
                    _stamina = Mathf.Min(maxStamina, _stamina + staminaRegen * Time.deltaTime);
            }
        }

        private void HandleCrouch()
        {
            bool wantsCrouch = Input.GetKey(KeyCode.LeftControl);
            _isCrouching = wantsCrouch;
            float target = _isCrouching ? crouchHeight : standHeight;
            _cc.height = Mathf.Lerp(_cc.height, target, Time.deltaTime * crouchTransition);
            _cc.center = new Vector3(0f, _cc.height * 0.5f, 0f);
        }

        private void HandleNoise()
        {
            _noiseTimer += Time.deltaTime;
            if (_noiseTimer < noiseBroadcastInterval) return;
            _noiseTimer = 0f;

            Vector3 flat = new Vector3(_velocity.x, 0f, _velocity.z);
            float speed = flat.magnitude;
            if (speed < 0.3f) return;

            float noise = _isCrouching ? crouchNoise : (_isRunning ? runNoise : walkNoise);
            Core.EventBus.Publish(new Core.EventBus.NoiseEvent
            {
                position = transform.position,
                radius = noise,
                intensity = Mathf.Clamp01(speed / runSpeed),
                source = gameObject
            });
        }
    }
}
