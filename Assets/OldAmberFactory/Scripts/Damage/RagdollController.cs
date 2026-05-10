using UnityEngine;

namespace OldAmberFactory.Damage
{
    /// <summary>
    /// Switches a humanoid between animated and ragdoll state.
    /// Call Activate() on death and pass the last hit for physics-accurate fall.
    /// </summary>
    public class RagdollController : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Rigidbody[] bodies;
        [SerializeField] private Collider[] colliders;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Behaviour[] behavioursToDisable;

        void Reset()
        {
            animator = GetComponentInChildren<Animator>();
            bodies = GetComponentsInChildren<Rigidbody>();
            colliders = GetComponentsInChildren<Collider>();
            characterController = GetComponent<CharacterController>();
        }

        void Start() { Deactivate(); }

        public void Deactivate()
        {
            foreach (var rb in bodies)
            {
                if (rb == null) continue;
                rb.isKinematic = true;
                rb.detectCollisions = false;
            }
            foreach (var col in colliders)
            {
                if (col == null) continue;
                // Keep the main body collider for hit detection, disable bone colliders that would interfere.
                if (col.gameObject == gameObject) continue;
                col.enabled = false;
            }
        }

        public void Activate(Vector3 impulse, Vector3 impactPoint)
        {
            if (animator) animator.enabled = false;
            foreach (var b in behavioursToDisable) if (b) b.enabled = false;
            if (characterController) characterController.enabled = false;

            foreach (var col in colliders) if (col) col.enabled = true;
            foreach (var rb in bodies)
            {
                if (rb == null) continue;
                rb.isKinematic = false;
                rb.detectCollisions = true;
            }

            // Find closest body to impact to apply impulse there.
            Rigidbody closest = null;
            float min = float.MaxValue;
            foreach (var rb in bodies)
            {
                if (rb == null) continue;
                float d = (rb.position - impactPoint).sqrMagnitude;
                if (d < min) { min = d; closest = rb; }
            }
            if (closest != null)
                closest.AddForceAtPosition(impulse, impactPoint, ForceMode.Impulse);
        }
    }
}
