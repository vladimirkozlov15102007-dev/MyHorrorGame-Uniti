using System.Collections.Generic;
using UnityEngine;
using OldAmberFactory.Player;

namespace OldAmberFactory.Environment
{
    /// <summary>
    /// Interactable security room: an array of RenderTextures fed by world cameras
    /// are displayed on CRT monitors. While watching, the player is pinned and vulnerable.
    /// </summary>
    public class SecurityRoom : MonoBehaviour, IInteractable
    {
        [SerializeField] private Camera[] securityCameras;
        [SerializeField] private Renderer monitorRenderer;
        [SerializeField] private GameObject overlayHUD;
        [SerializeField] private Transform viewAnchor;
        [SerializeField] private float useDuration = 25f;
        [SerializeField] private float cooldown = 45f;

        private int _activeCameraIndex;
        private float _timer;
        private float _cooldownTimer;
        private bool _active;
        private Transform _player;

        public string Prompt => _cooldownTimer > 0f ? $"CCTV rebooting ({_cooldownTimer:F0}s)"
                              : _active ? "Press E to exit" : "Monitor CCTV feeds (E)";
        public bool CanInteract(GameObject interactor) => _cooldownTimer <= 0f;

        public void Interact(GameObject interactor)
        {
            if (_active) Stop();
            else Start(interactor.transform);
        }

        void Start_Anchor() {}

        void Start(Transform player)
        {
            _active = true;
            _player = player;
            _timer = useDuration;
            if (overlayHUD) overlayHUD.SetActive(true);
            ActivateCamera(0);
        }

        void Stop()
        {
            _active = false;
            _player = null;
            if (overlayHUD) overlayHUD.SetActive(false);
            DeactivateAll();
            _cooldownTimer = cooldown;
        }

        void Update()
        {
            _cooldownTimer = Mathf.Max(0f, _cooldownTimer - Time.deltaTime);
            if (!_active) return;

            _timer -= Time.deltaTime;
            if (_timer <= 0f) { Stop(); return; }

            if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Q))
                ActivateCamera((_activeCameraIndex - 1 + securityCameras.Length) % securityCameras.Length);
            if (Input.GetKeyDown(KeyCode.E) == false && Input.GetKeyDown(KeyCode.F))
                ActivateCamera((_activeCameraIndex + 1) % securityCameras.Length);

            // Pin the player at anchor while active (cannot move).
            if (_player && viewAnchor)
            {
                _player.position = viewAnchor.position;
            }
        }

        void ActivateCamera(int idx)
        {
            if (securityCameras == null || securityCameras.Length == 0) return;
            DeactivateAll();
            _activeCameraIndex = Mathf.Clamp(idx, 0, securityCameras.Length - 1);
            securityCameras[_activeCameraIndex].enabled = true;
        }

        void DeactivateAll()
        {
            if (securityCameras == null) return;
            foreach (var c in securityCameras) if (c) c.enabled = false;
        }
    }
}
