using System.Collections.Generic;
using OldAmberFactory.Damage;
using UnityEngine;
using UnityEngine.UI;

namespace OldAmberFactory.UI
{
    /// <summary>
    /// Spawns arrow-style indicators around the crosshair pointing to where damage came from.
    /// Subscribes to the player's HealthSystem.OnDamaged. Indicators fade over time.
    /// </summary>
    public class DirectionalDamageIndicator : MonoBehaviour
    {
        [SerializeField] private HealthSystem _playerHealth;
        [SerializeField] private Transform _playerTransform;
        [SerializeField] private Camera _camera;
        [SerializeField] private RectTransform _canvas;
        [SerializeField] private Image _indicatorPrefab;
        [SerializeField] private float _lifetime = 1.4f;
        [SerializeField] private float _radius = 160f;

        private class Indicator
        {
            public Image image;
            public Vector3 worldHitDir;
            public float bornAt;
        }

        private readonly List<Indicator> _active = new();

        private void OnEnable()
        {
            if (_playerHealth != null) _playerHealth.OnDamaged += OnPlayerDamaged;
        }
        private void OnDisable()
        {
            if (_playerHealth != null) _playerHealth.OnDamaged -= OnPlayerDamaged;
        }

        private void OnPlayerDamaged(DamageInfo info)
        {
            if (_indicatorPrefab == null || _canvas == null) return;
            var img = Instantiate(_indicatorPrefab, _canvas);
            _active.Add(new Indicator { image = img, worldHitDir = -info.hitDirection, bornAt = Time.time });
        }

        private void LateUpdate()
        {
            if (_camera == null) return;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var it = _active[i];
                float t = (Time.time - it.bornAt) / _lifetime;
                if (t >= 1f || it.image == null)
                {
                    if (it.image != null) Destroy(it.image.gameObject);
                    _active.RemoveAt(i);
                    continue;
                }

                // Project hit dir into camera space to get screen-space angle.
                Vector3 camSpace = _camera.transform.InverseTransformDirection(it.worldHitDir);
                float angle = Mathf.Atan2(camSpace.x, camSpace.z) * Mathf.Rad2Deg;

                var rt = it.image.rectTransform;
                rt.anchoredPosition = new Vector2(
                    Mathf.Sin(angle * Mathf.Deg2Rad) * _radius,
                    Mathf.Cos(angle * Mathf.Deg2Rad) * _radius
                );
                rt.localRotation = Quaternion.Euler(0f, 0f, -angle);
                var c = it.image.color;
                c.a = 1f - t;
                it.image.color = c;
            }
        }
    }
}
