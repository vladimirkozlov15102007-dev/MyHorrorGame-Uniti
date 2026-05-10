using UnityEngine;

namespace OldAmberFactory.Environment
{
    /// <summary>
    /// Simple destructible prop. When HP drops to 0, swaps to a pre-fractured version
    /// (pre-baked in Blender / with Unity's Mesh API) and disables the pristine mesh.
    /// </summary>
    [RequireComponent(typeof(Damage.Health))]
    public class DestructibleProp : MonoBehaviour
    {
        [SerializeField] private GameObject pristine;
        [SerializeField] private GameObject shattered;
        [SerializeField] private AudioClip breakClip;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private float debrisImpulse = 2.5f;

        Damage.Health _health;
        void Awake()
        {
            _health = GetComponent<Damage.Health>();
            if (shattered) shattered.SetActive(false);
        }

        void OnEnable() { _health.OnDied += HandleDied; }
        void OnDisable() { _health.OnDied -= HandleDied; }

        void HandleDied(Damage.DamageInfo info)
        {
            if (pristine) pristine.SetActive(false);
            if (shattered)
            {
                shattered.SetActive(true);
                foreach (var rb in shattered.GetComponentsInChildren<Rigidbody>())
                    rb.AddForce(info.hitDirection * debrisImpulse, ForceMode.Impulse);
            }
            if (breakClip && audioSource) audioSource.PlayOneShot(breakClip);

            Core.EventBus.Publish(new Core.EventBus.NoiseEvent
            {
                position = transform.position,
                radius = 12f,
                intensity = 0.7f,
                source = gameObject
            });
        }
    }
}
