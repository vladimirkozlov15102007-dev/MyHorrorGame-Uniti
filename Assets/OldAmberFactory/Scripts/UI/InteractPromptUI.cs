using OldAmberFactory.Core;
using OldAmberFactory.Player;
using TMPro;
using UnityEngine;

namespace OldAmberFactory.UI
{
    /// <summary>Small floating label showing the current interactable prompt, fades in/out.</summary>
    public class InteractPromptUI : MonoBehaviour
    {
        [SerializeField] private PlayerInteractor _interactor;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private float _fade = 8f;

        private void Update()
        {
            if (_interactor == null || _label == null || _group == null) return;

            IInteractable cur = _interactor.Current;
            bool show = cur != null && cur.CanInteract(_interactor.gameObject);
            if (show) _label.text = $"[E] {cur.Prompt}";
            _group.alpha = Mathf.MoveTowards(_group.alpha, show ? 1f : 0f, _fade * Time.deltaTime);
        }
    }
}
