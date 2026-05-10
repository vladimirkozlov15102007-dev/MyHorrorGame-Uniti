using UnityEngine;

namespace OldAmberFactory.Throwables
{
    /// <summary>
    /// Holds a single throwable in the player's hand. Allows LMB charging and release.
    /// </summary>
    public class ThrowableHolder : MonoBehaviour
    {
        [SerializeField] private Transform handSocket;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float maxChargeTime = 1.4f;
        [SerializeField] private float minForce = 6f;
        [SerializeField] private float maxForce = 22f;

        public bool HasThrowable => _current != null;
        public float ChargeNormalized => Mathf.Clamp01(_charge / maxChargeTime);

        private Throwable _current;
        private Rigidbody _currentRb;
        private float _charge;

        public void PickUp(Throwable t)
        {
            if (_current != null) return;
            _current = t;
            _currentRb = t.GetComponent<Rigidbody>();
            _currentRb.isKinematic = true;
            _currentRb.detectCollisions = false;
            t.transform.SetParent(handSocket);
            t.transform.localPosition = Vector3.zero;
            t.transform.localRotation = Quaternion.identity;
        }

        public void Charge(float dt) { if (HasThrowable) _charge = Mathf.Min(maxChargeTime, _charge + dt); }

        public void Release()
        {
            if (!HasThrowable) return;
            float t = ChargeNormalized;
            float force = Mathf.Lerp(minForce, maxForce, t);
            _current.transform.SetParent(null);
            _currentRb.isKinematic = false;
            _currentRb.detectCollisions = true;
            Vector3 dir = playerCamera ? playerCamera.transform.forward : handSocket.forward;
            _currentRb.linearVelocity = Vector3.zero;
            _currentRb.AddForce(dir * force, ForceMode.VelocityChange);
            _currentRb.AddTorque(Random.insideUnitSphere * 6f, ForceMode.VelocityChange);
            _current = null;
            _currentRb = null;
            _charge = 0f;
        }

        public void DropSilent()
        {
            if (!HasThrowable) return;
            _current.transform.SetParent(null);
            _currentRb.isKinematic = false;
            _currentRb.detectCollisions = true;
            _current = null;
            _currentRb = null;
            _charge = 0f;
        }
    }
}
