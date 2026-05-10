using OldAmberFactory.Damage;
using OldAmberFactory.Player;
using OldAmberFactory.Weapons;
using OldAmberFactory.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OldAmberFactory.UI
{
    /// <summary>
    /// Minimalist diegetic-style HUD. Only HP, stamina, ammo and objective text.
    /// Hidden during cutscenes / ending.
    /// </summary>
    public class HUDView : MonoBehaviour
    {
        [Header("Bindings")]
        [SerializeField] private HealthSystem _playerHealth;
        [SerializeField] private PlayerStamina _stamina;
        [SerializeField] private WeaponBase _weapon;
        [SerializeField] private GameManager _gameManager;

        [Header("Widgets")]
        [SerializeField] private Image _healthFill;
        [SerializeField] private Image _staminaFill;
        [SerializeField] private TMP_Text _ammoText;
        [SerializeField] private TMP_Text _objectiveText;
        [SerializeField] private TMP_Text _skeletonCountText;
        [SerializeField] private CanvasGroup _root;

        private void Start()
        {
            if (_playerHealth != null)
                _playerHealth.OnHealthChanged += (cur, max) => UpdateFill(_healthFill, cur, max);
            if (_stamina != null)
                _stamina.OnChanged += (cur, max) => UpdateFill(_staminaFill, cur, max);
            if (_gameManager != null)
                _gameManager.OnSkeletonsRemainingChanged += UpdateSkeletonCount;
        }

        private void Update()
        {
            if (_weapon != null && _ammoText != null)
                _ammoText.text = $"{_weapon.Ammo:00}/{_weapon.Reserve:000}";
        }

        public void SetVisible(bool visible)
        {
            if (_root != null) _root.alpha = visible ? 1f : 0f;
        }

        public void SetObjective(string text)
        {
            if (_objectiveText != null) _objectiveText.text = text;
        }

        private void UpdateFill(Image img, float cur, float max)
        {
            if (img != null) img.fillAmount = max <= 0.01f ? 0f : cur / max;
        }

        private void UpdateSkeletonCount(int n)
        {
            if (_skeletonCountText != null)
                _skeletonCountText.text = n > 0 ? $"Skeletons: {n}" : "Area cleared";
        }
    }
}
