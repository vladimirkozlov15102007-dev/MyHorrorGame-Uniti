using UnityEngine;

namespace OldAmberFactory.Damage
{
    /// <summary>
    /// Attach to bones/colliders of a character. Forwards damage to owning Health.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Hitbox : MonoBehaviour
    {
        [SerializeField] private BodyPart bodyPart = BodyPart.Generic;
        [SerializeField] private Health health;

        public BodyPart Part => bodyPart;
        public Health Owner => health;

        void Reset()
        {
            health = GetComponentInParent<Health>();
        }

        public void Receive(DamageInfo info)
        {
            info.bodyPart = bodyPart;
            if (health == null) health = GetComponentInParent<Health>();
            if (health != null) health.ApplyDamage(info);
        }
    }
}
