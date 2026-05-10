using System;
using UnityEngine;

namespace OldAmberFactory.Damage
{
    /// <summary>
    /// Generic health container used by both the player and enemy skeletons.
    /// Design: player 100 HP, skeleton 100 HP.
    /// </summary>
    public class HealthSystem : MonoBehaviour, IDamageReceiver
    {
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _currentHealth = 100f;
        [SerializeField] private bool _invulnerable;

        public event Action<DamageInfo> OnDamaged;
        public event Action<DamageInfo> OnDied;
        public event Action<float,float> OnHealthChanged; // (current, max)

        public float Current   => _currentHealth;
        public float Max       => _maxHealth;
        public float Normalized => _maxHealth <= 0.01f ? 0f : _currentHealth / _maxHealth;
        public bool IsDead     { get; private set; }

        private void Awake()
        {
            _currentHealth = _maxHealth;
        }

        public void SetInvulnerable(bool on) => _invulnerable = on;

        public void ReceiveDamage(DamageInfo info)
        {
            if (IsDead || _invulnerable) return;

            _currentHealth = Mathf.Max(0f, _currentHealth - info.amount);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            OnDamaged?.Invoke(info);

            if (_currentHealth <= 0f)
            {
                IsDead = true;
                OnDied?.Invoke(info);
            }
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }
    }
}
