using UnityEngine;

namespace OldAmberFactory.Damage
{
    /// <summary>
    /// Attached to every collider that represents an anatomical hit zone.
    /// Forwards damage to its owning IDamageReceiver, applying a per-zone multiplier.
    /// Design spec: head = 30, torso = 20, legs = 10 (for a 1.0 base shot).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class HitBox : MonoBehaviour
    {
        [SerializeField] private BodyPartType _part = BodyPartType.Torso;
        [SerializeField] private float _damageMultiplier = 1f;
        [SerializeField] private MonoBehaviour _ownerReceiver; // must implement IDamageReceiver
        [SerializeField] private Rigidbody _ragdollBody;       // bone body that takes the impulse

        public BodyPartType Part => _part;
        public Rigidbody RagdollBody => _ragdollBody;

        public void Receive(DamageInfo info)
        {
            info.amount *= _damageMultiplier;
            info.bodyPart = _part;

            if (_ragdollBody != null && info.impulse > 0f)
            {
                _ragdollBody.AddForceAtPosition(info.hitDirection * info.impulse,
                    info.hitPoint, ForceMode.Impulse);
            }

            if (_ownerReceiver is IDamageReceiver dr)
                dr.ReceiveDamage(info);
        }
    }
}
