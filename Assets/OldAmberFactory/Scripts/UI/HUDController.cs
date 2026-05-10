using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OldAmberFactory.Player;
using OldAmberFactory.Weapons;

namespace OldAmberFactory.UI
{
    /// <summary>
    /// Minimalist HUD: health, stamina, ammo, interaction prompt.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private Damage.Health playerHealth;
        [SerializeField] private WeaponController weaponController;
        [SerializeField] private Interactor interactor;

        [SerializeField] private Image healthFill;
        [SerializeField] private Image staminaFill;
        [SerializeField] private TMP_Text ammoText;
        [SerializeField] private TMP_Text promptText;
        [SerializeField] private TMP_Text objectiveText;
        [SerializeField] private CanvasGroup damageFlash;

        void Awake()
        {
            if (playerHealth) playerHealth.OnDamaged += _ => FlashDamage();
        }

        void Update()
        {
            if (healthFill && playerHealth) healthFill.fillAmount = playerHealth.Normalized;
            if (staminaFill && player) staminaFill.fillAmount = player.StaminaNormalized;

            if (ammoText && weaponController && weaponController.Weapon)
                ammoText.text = $"{weaponController.Weapon.CurrentMag:D2} / {weaponController.Weapon.Reserve:D2}";

            if (promptText && interactor)
                promptText.text = interactor.CurrentPrompt ?? string.Empty;

            if (objectiveText) objectiveText.text = BuildObjective();

            if (damageFlash)
                damageFlash.alpha = Mathf.MoveTowards(damageFlash.alpha, 0f, Time.deltaTime * 1.8f);
        }

        string BuildObjective()
        {
            var gm = Core.GameManager.Instance;
            if (gm == null) return "";
            if (gm.State == Core.GameManager.GameState.Victory) return "ESCAPED";
            if (gm.State == Core.GameManager.GameState.GameOver) return "YOU DIED";
            if (gm.SkeletonsAlive > 0) return $"Survive: Skeletons remaining {gm.SkeletonsAlive}";
            if (!gm.TruckPowerActivated) return "Find the factory power switch";
            if (!gm.TruckKeyFound) return "Find the truck key";
            if (!gm.TruckStarted) return "Reach the Yellow Truck and start it";
            return "Escape!";
        }

        void FlashDamage()
        {
            if (damageFlash) damageFlash.alpha = 0.55f;
            CameraShake.Instance?.Kick(0.4f);
        }
    }
}
