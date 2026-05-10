using OldAmberFactory.Core;
using OldAmberFactory.Player;
using UnityEngine;

namespace OldAmberFactory.Throwable
{
    /// <summary>
    /// Player-side throw controller. When an interactable ThrowableItem is in range:
    ///   - E picks it up into a hand socket
    ///   - LMB hold charges the throw strength
    ///   - LMB release throws with a velocity proportional to charge
    /// </summary>
    public class ThrowController : MonoBehaviour
    {
        [SerializeField] private InputRouter _input;
        [SerializeField] private PlayerInteractor _interactor;
        [SerializeField] private Camera _aimCamera;
        [SerializeField] private Transform _handSocket;
        [SerializeField] private float _minThrowSpeed = 4f;
        [SerializeField] private float _maxThrowSpeed = 18f;
        [SerializeField] private float _maxChargeTime = 1.2f;

        private ThrowableItem _held;
        private float _chargeStart = -1f;

        public bool HasItem => _held != null;

        private void Update()
        {
            if (_input == null) return;

            if (_held == null)
            {
                // pickup on interact ray
                if (_input.InteractPressed && _interactor?.Current is ThrowablePickup p)
                    Pickup(p.Item);
                return;
            }

            if (_input.FirePressed) _chargeStart = Time.time;

            if (_input.FireReleased && _chargeStart > 0f)
            {
                float t = Mathf.Clamp01((Time.time - _chargeStart) / _maxChargeTime);
                float speed = Mathf.Lerp(_minThrowSpeed, _maxThrowSpeed, t);
                Vector3 dir = _aimCamera != null ? _aimCamera.transform.forward : transform.forward;

                _held.Throw(dir * speed);
                _held = null;
                _chargeStart = -1f;
            }
        }

        private void Pickup(ThrowableItem item)
        {
            _held = item;
            item.SetHeld(true, _handSocket);
        }
    }

    /// <summary>
    /// Simple interactable wrapper so PlayerInteractor can offer "Pick up" on ThrowableItems.
    /// </summary>
    public class ThrowablePickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private ThrowableItem _item;
        public ThrowableItem Item => _item;
        public string Prompt => "Pick up";
        public bool CanInteract(GameObject user) => _item != null;
        public void Interact(GameObject user) {/* pickup handled by ThrowController */}
    }
}
