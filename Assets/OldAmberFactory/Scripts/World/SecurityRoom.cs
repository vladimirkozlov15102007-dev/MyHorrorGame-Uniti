using OldAmberFactory.Core;
using UnityEngine;

namespace OldAmberFactory.World
{
    /// <summary>
    /// CRT camera bank. The player looks at the monitors (interactable) and presses E:
    ///   - First press: enter surveillance mode (camera 0 output rendered on the CRT).
    ///   - Subsequent presses: cycle to the next camera.
    ///   - After cycling past the last camera, surveillance session ends and cooldown starts.
    ///   - Session also auto-ends after _maxUseTime.
    ///
    /// While in use, the player is flagged "vulnerable" — an external damage modifier can
    /// multiply incoming damage for that period. Since the room is also the safest place
    /// from direct fire, this rewards short peeks rather than constant monitoring.
    /// </summary>
    public class SecurityRoom : MonoBehaviour, IInteractable
    {
        [SerializeField] private SecurityCamera[] _cameras;
        [SerializeField] private MeshRenderer _screenRenderer;
        [SerializeField] private float _maxUseTime = 25f;
        [SerializeField] private float _cooldown = 20f;
        [SerializeField] private float _vulnerableBonus = 1.5f;

        private int _active = -1;
        private bool _inUse;
        private float _sessionEndsAt;
        private float _readyAt;

        public bool IsPlayerVulnerable => _inUse;
        public float VulnerableDamageMultiplier => _vulnerableBonus;

        public string Prompt
        {
            get
            {
                if (Time.time < _readyAt) return "Cameras offline (cooldown)";
                if (!_inUse)               return "View security cameras";
                if (_active < _cameras.Length - 1) return $"Switch camera ({_active + 2}/{_cameras.Length})";
                return "Exit surveillance";
            }
        }

        public bool CanInteract(GameObject user) => Time.time >= _readyAt;

        public void Interact(GameObject user)
        {
            if (!_inUse) { Enter(); return; }

            _active++;
            if (_active >= _cameras.Length) { Exit(); return; }
            ApplyScreen(_cameras[_active].Output);
        }

        private void Update()
        {
            if (_inUse && Time.time > _sessionEndsAt) Exit();
        }

        private void Enter()
        {
            if (_cameras == null || _cameras.Length == 0) return;
            _inUse = true;
            _active = 0;
            _sessionEndsAt = Time.time + _maxUseTime;
            for (int i = 0; i < _cameras.Length; i++) _cameras[i].SetRendering(true);
            ApplyScreen(_cameras[_active].Output);
        }

        private void Exit()
        {
            _inUse = false;
            _active = -1;
            _readyAt = Time.time + _cooldown;
            for (int i = 0; i < _cameras.Length; i++) _cameras[i].SetRendering(false);
            ApplyScreen(null);
        }

        private void ApplyScreen(RenderTexture rt)
        {
            if (_screenRenderer == null) return;
            if (_screenRenderer.material.HasProperty("_BaseMap"))
                _screenRenderer.material.SetTexture("_BaseMap", rt);
            _screenRenderer.material.mainTexture = rt;
        }
    }
}
