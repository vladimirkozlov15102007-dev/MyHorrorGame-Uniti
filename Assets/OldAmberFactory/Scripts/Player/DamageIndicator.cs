using UnityEngine;
using UnityEngine.UI;
using OldAmberFactory.Damage;

namespace OldAmberFactory.Player
{
    /// <summary>
    /// Spawns directional damage markers around the HUD when the player takes damage.
    /// Markers fade out over time.
    /// </summary>
    public class DamageIndicator : MonoBehaviour
    {
        [SerializeField] private Health playerHealth;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private RectTransform container;
        [SerializeField] private Image markerPrefab;
        [SerializeField] private float duration = 1.4f;
        [SerializeField] private float ringRadius = 160f;

        void OnEnable()  { if (playerHealth) playerHealth.OnDamaged += HandleDamaged; }
        void OnDisable() { if (playerHealth) playerHealth.OnDamaged -= HandleDamaged; }

        void HandleDamaged(DamageInfo info)
        {
            if (markerPrefab == null || container == null || playerTransform == null) return;
            Vector3 dir = info.hitPoint - playerTransform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return;
            float angle = Vector3.SignedAngle(playerTransform.forward, dir.normalized, Vector3.up);

            var marker = Instantiate(markerPrefab, container);
            var rt = marker.rectTransform;
            rt.anchoredPosition = new Vector2(Mathf.Sin(angle * Mathf.Deg2Rad), Mathf.Cos(angle * Mathf.Deg2Rad)) * ringRadius;
            rt.localRotation = Quaternion.Euler(0f, 0f, -angle);

            StartCoroutine(Fade(marker));
        }

        System.Collections.IEnumerator Fade(Image m)
        {
            float t = 0f;
            var col = m.color;
            while (t < duration)
            {
                t += Time.deltaTime;
                m.color = new Color(col.r, col.g, col.b, Mathf.Lerp(col.a, 0f, t / duration));
                yield return null;
            }
            Destroy(m.gameObject);
        }
    }
}
