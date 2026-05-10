using System;
using UnityEngine;

namespace OldAmberFactory.Player
{
    /// <summary>
    /// Stamina pool consumed by sprinting, jumping and hard actions.
    /// Regens after a short delay; drains at a tuned rate when sprinting.
    /// </summary>
    public class PlayerStamina : MonoBehaviour
    {
        [SerializeField] private float _max = 100f;
        [SerializeField] private float _drainPerSecond = 22f;
        [SerializeField] private float _regenPerSecond = 18f;
        [SerializeField] private float _regenDelay = 1.4f;

        private float _current;
        private float _nextRegenAt;

        public float Current => _current;
        public float Max => _max;
        public float Normalized => _max <= 0.01f ? 0f : _current / _max;
        public bool HasStamina => _current > 1f;

        public event Action<float,float> OnChanged;

        private void Awake() => _current = _max;

        public void Drain(float dt)
        {
            _current = Mathf.Max(0f, _current - _drainPerSecond * dt);
            _nextRegenAt = Time.time + _regenDelay;
            OnChanged?.Invoke(_current, _max);
        }

        public void Regen(float dt)
        {
            if (Time.time < _nextRegenAt) return;
            if (_current >= _max) return;
            _current = Mathf.Min(_max, _current + _regenPerSecond * dt);
            OnChanged?.Invoke(_current, _max);
        }

        public bool TrySpend(float amount)
        {
            if (_current < amount) return false;
            _current -= amount;
            _nextRegenAt = Time.time + _regenDelay;
            OnChanged?.Invoke(_current, _max);
            return true;
        }
    }
}
