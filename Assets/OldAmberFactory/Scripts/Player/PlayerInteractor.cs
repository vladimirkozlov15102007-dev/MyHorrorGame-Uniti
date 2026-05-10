using OldAmberFactory.Core;
using UnityEngine;

namespace OldAmberFactory.Player
{
    /// <summary>
    /// Casts a ray from the camera each frame, highlights the current interactable,
    /// and triggers its Interact() on the Interact input.
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private InputRouter _input;
        [SerializeField] private float _range = 2.6f;
        [SerializeField] private LayerMask _mask = ~0;

        private IInteractable _current;

        public IInteractable Current => _current;

        private void Update()
        {
            _current = null;
            if (_camera == null || _input == null) return;

            Ray r = new Ray(_camera.transform.position, _camera.transform.forward);
            if (Physics.Raycast(r, out var hit, _range, _mask, QueryTriggerInteraction.Collide))
            {
                _current = hit.collider.GetComponentInParent<IInteractable>();
            }

            if (_current != null && _input.InteractPressed)
                _current.Interact(gameObject);
        }
    }
}
