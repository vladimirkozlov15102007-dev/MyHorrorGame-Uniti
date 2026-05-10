using System;
using UnityEngine;

namespace OldAmberFactory.Damage
{
    /// <summary>
    /// Health component. Receives DamageInfo and raises events.
    /// Base damage convention: 10 -> Head 30, Torso 20, Legs 10 (via multipliers).
    /// </summary>
    public class Health : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth = 100f;
        [SerializeField] private bool invulnerable;

        public float Max => maxHealth;
        public float Current => currentHealth;
        public float Normalized => Mathf.Clamp01(currentHealth / maxHealth);
        public bool IsDead => currentHealth <= 0f;

        public event Action<DamageInfo> OnDamaged;
        public event Action<DamageInfo> OnDied;
        public event Action<float> OnHealthChanged;

        void Awake() { currentHealth = maxHealth; }

        public void SetInvulnerable(bool value) => invulnerable = value;

        public void ApplyDamage(DamageInfo info)
        {
            if (IsDead || invulnerable) return;
            float dmg = info.FinalDamage();
            currentHealth = Mathf.Max(0f, currentHealth - dmg);
            OnHealthChanged?.Invoke(Normalized);
            OnDamaged?.Invoke(info);
            if (currentHealth <= 0f) OnDied?.Invoke(info);
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(Normalized);
        }
    }
}
