using UnityEngine;
using UnityEngine.InputSystem;

namespace OldAmberFactory.Player
{
    /// <summary>
    /// Single point that reads the Input System and exposes polled values + one-shot events.
    /// Keeps the rest of the player code free of direct InputSystem types.
    /// Expected action map (see Input/PlayerControls.inputactions):
    ///   Move (Vector2) WASD
    ///   Look (Vector2) mouse delta
    ///   Sprint (Button, hold) Shift
    ///   Jump (Button) Space
    ///   Crouch (Button, toggle-on-press) Ctrl
    ///   Interact (Button) E
    ///   Fire (Button) LMB
    ///   Aim (Button, hold) RMB
    ///   Reload (Button) R
    ///   Throw (Button, hold to charge) LMB while throwable equipped
    /// </summary>
    public class InputRouter : MonoBehaviour
    {
        [SerializeField] private InputActionAsset _asset;

        private InputAction _move, _look, _sprint, _jump, _crouch, _interact, _fire, _aim, _reload;

        public Vector2 Move   => _move   != null ? _move.ReadValue<Vector2>() : Vector2.zero;
        public Vector2 Look   => _look   != null ? _look.ReadValue<Vector2>() : Vector2.zero;
        public bool Sprint    => _sprint != null && _sprint.IsPressed();
        public bool Aim       => _aim    != null && _aim.IsPressed();
        public bool FireHeld  => _fire   != null && _fire.IsPressed();

        public bool JumpPressed      { get; private set; }
        public bool CrouchPressed    { get; private set; }
        public bool InteractPressed  { get; private set; }
        public bool FirePressed      { get; private set; }
        public bool FireReleased     { get; private set; }
        public bool ReloadPressed    { get; private set; }

        private void Awake()
        {
            if (_asset == null) return;
            var map = _asset.FindActionMap("Player", throwIfNotFound: false);
            if (map == null) return;

            _move     = map.FindAction("Move");
            _look     = map.FindAction("Look");
            _sprint   = map.FindAction("Sprint");
            _jump     = map.FindAction("Jump");
            _crouch   = map.FindAction("Crouch");
            _interact = map.FindAction("Interact");
            _fire     = map.FindAction("Fire");
            _aim      = map.FindAction("Aim");
            _reload   = map.FindAction("Reload");
        }

        private void OnEnable()  { _asset?.Enable(); }
        private void OnDisable() { _asset?.Disable(); }

        private void Update()
        {
            JumpPressed     = _jump     != null && _jump.WasPressedThisFrame();
            CrouchPressed   = _crouch   != null && _crouch.WasPressedThisFrame();
            InteractPressed = _interact != null && _interact.WasPressedThisFrame();
            FirePressed     = _fire     != null && _fire.WasPressedThisFrame();
            FireReleased    = _fire     != null && _fire.WasReleasedThisFrame();
            ReloadPressed   = _reload   != null && _reload.WasPressedThisFrame();
        }
    }
}
