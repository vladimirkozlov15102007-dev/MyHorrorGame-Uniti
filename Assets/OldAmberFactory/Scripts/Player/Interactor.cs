using UnityEngine;

namespace OldAmberFactory.Player
{
    public interface IInteractable
    {
        string Prompt { get; }
        bool CanInteract(GameObject interactor);
        void Interact(GameObject interactor);
    }

    /// <summary>
    /// Raycasts from the camera to find interactables. Press E to interact.
    /// </summary>
    public class Interactor : MonoBehaviour
    {
        [SerializeField] private Transform rayOrigin;
        [SerializeField] private float range = 2.8f;
        [SerializeField] private LayerMask interactableMask = ~0;

        public IInteractable CurrentTarget { get; private set; }
        public string CurrentPrompt { get; private set; }

        void Update()
        {
            CurrentTarget = null;
            CurrentPrompt = null;
            if (rayOrigin == null) return;

            if (Physics.Raycast(rayOrigin.position, rayOrigin.forward, out var hit, range, interactableMask,
                QueryTriggerInteraction.Collide))
            {
                var i = hit.collider.GetComponentInParent<IInteractable>();
                if (i != null && i.CanInteract(gameObject))
                {
                    CurrentTarget = i;
                    CurrentPrompt = i.Prompt;
                    if (Input.GetKeyDown(KeyCode.E)) i.Interact(gameObject);
                }
            }
        }
    }
}
