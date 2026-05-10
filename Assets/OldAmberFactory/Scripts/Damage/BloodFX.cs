using UnityEngine;

namespace OldAmberFactory.Damage
{
    /// <summary>
    /// Spawns blood VFX and a decal at hit points. Hook to Health.OnDamaged.
    /// </summary>
    public class BloodFX : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private GameObject bloodSprayPrefab;
        [SerializeField] private GameObject bloodDecalPrefab;
        [SerializeField] private float decalLifetime = 30f;

        void Reset() { health = GetComponent<Health>(); }

        void OnEnable()
        {
            if (health) health.OnDamaged += HandleDamaged;
        }

        void OnDisable()
        {
            if (health) health.OnDamaged -= HandleDamaged;
        }

        void HandleDamaged(DamageInfo info)
        {
            if (bloodSprayPrefab)
            {
                var rot = Quaternion.LookRotation(-info.hitDirection.normalized);
                var fx = Instantiate(bloodSprayPrefab, info.hitPoint, rot);
                Destroy(fx, 3f);
            }

            // Raycast to wall behind to place a decal.
            if (bloodDecalPrefab && Physics.Raycast(info.hitPoint, info.hitDirection, out var hit, 3f,
                ~0, QueryTriggerInteraction.Ignore))
            {
                var decal = Instantiate(bloodDecalPrefab, hit.point + hit.normal * 0.01f,
                    Quaternion.LookRotation(-hit.normal));
                Destroy(decal, decalLifetime);
            }
        }
    }
}
