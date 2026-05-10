using OldAmberFactory.Core;
using UnityEngine;
using UnityEngine.Events;

namespace OldAmberFactory.World
{
    /// <summary>
    /// The escape objective. Requires three staged interactions:
    ///   1) Power switched on (fuse, flipped inside the Administration zone).
    ///   2) Ignition key in inventory (looted from a corpse / locker).
    ///   3) E on the driver door — starts the engine, triggers the final wave
    ///      and the ending sequence once the player survives.
    /// </summary>
    public class YellowTruck : MonoBehaviour, IInteractable
    {
        [SerializeField] private UnityEvent _onPowerActivated;
        [SerializeField] private UnityEvent _onKeyInserted;
        [SerializeField] private UnityEvent _onEngineStart;
        [SerializeField] private UnityEvent _onEscape;

        [SerializeField] private AudioSource _engineSource;
        [SerializeField] private AudioClip _engineStartClip;
        [SerializeField] private AudioClip _engineLoopClip;
        [SerializeField] private AudioClip _failClickClip;
        [SerializeField] private Animator _ignitionAnimator;
        [SerializeField] private float _escapeDriveTime = 8f;
        [SerializeField] private Transform _escapeTargetPoint;

        private bool _powerOn;
        private bool _keyInserted;
        private bool _engineRunning;

        public bool IsPowerOn => _powerOn;
        public bool HasKey    => _keyInserted;
        public bool IsEngineRunning => _engineRunning;

        public string Prompt
        {
            get
            {
                if (_engineRunning) return "Drive away";
                if (!_powerOn)      return "Power offline";
                if (!_keyInserted)  return "Insert ignition key";
                return "Start engine";
            }
        }

        public bool CanInteract(GameObject user) => true;

        public void Interact(GameObject user)
        {
            if (!_powerOn || !_keyInserted)
            {
                _engineSource?.PlayOneShot(_failClickClip);
                return;
            }
            if (!_engineRunning) StartEngine();
            else                 BeginEscape();
        }

        public void SetPower(bool on)
        {
            _powerOn = on;
            if (on) _onPowerActivated?.Invoke();
        }

        public void InsertKey()
        {
            _keyInserted = true;
            _onKeyInserted?.Invoke();
        }

        private void StartEngine()
        {
            _engineRunning = true;
            _ignitionAnimator?.SetTrigger("Start");
            if (_engineSource != null && _engineStartClip != null)
                _engineSource.PlayOneShot(_engineStartClip);
            _onEngineStart?.Invoke();

            var gm = ServiceLocator.Get<GameManager>();
            gm?.OnTruckEngineStarted();
        }

        private void BeginEscape()
        {
            _onEscape?.Invoke();
            if (_engineSource != null && _engineLoopClip != null)
            {
                _engineSource.clip = _engineLoopClip;
                _engineSource.loop = true;
                _engineSource.Play();
            }
            var gm = ServiceLocator.Get<GameManager>();
            gm?.OnTruckDriveAway(_escapeTargetPoint != null ? _escapeTargetPoint.position : transform.position + transform.forward * 40f,
                                 _escapeDriveTime);
        }
    }
}
