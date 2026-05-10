using UnityEngine;
using OldAmberFactory.Player;

namespace OldAmberFactory.Throwables
{
    /// <summary>
    /// Picks up physical objects (bottles, pipes, cans, rebar, stones) and emits noise events on impact.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class Throwable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string displayName = "Bottle";
        [SerializeField] private float noiseRadius = 14f;
        [SerializeField] private float noiseIntensity = 0.8f;
        [SerializeField] private AudioClip[] impactClips;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private float minImpactSpeed = 1.5f;

        public string Prompt => $"Pick up {displayName} (E)";

        public bool CanInteract(GameObject interactor) => true;
        public void Interact(GameObject interactor)
        {
            var holder = interactor.GetComponentInChildren<ThrowableHolder>();
            if (holder != null) holder.PickUp(this);
        }

        void OnCollisionEnter(Collision c)
        {
            if (c.relativeVelocity.magnitude < minImpactSpeed) return;
            if (impactClips != null && impactClips.Length > 0 && audioSource)
                audioSource.PlayOneShot(impactClips[Random.Range(0, impactClips.Length)], Mathf.Clamp01(c.relativeVelocity.magnitude / 10f));

            Core.EventBus.Publish(new Core.EventBus.NoiseEvent
            {
                position = transform.position,
                radius = noiseRadius,
                intensity = Mathf.Clamp01(noiseIntensity * c.relativeVelocity.magnitude / 10f),
                source = gameObject
            });
        }
    }
}
