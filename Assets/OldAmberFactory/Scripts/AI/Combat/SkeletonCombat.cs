using UnityEngine;

namespace OldAmberFactory.AI
{
    /// <summary>
    /// Handles bow shots and melee attacks for a skeleton. The BehaviorTree calls
    /// TryRangedShot / TryMelee. Animation hooks (AnimEvent_Release / AnimEvent_Melee)
    /// can be used to time the actual projectile spawn or damage.
    /// </summary>
    public class SkeletonCombat : MonoBehaviour
    {
        [Header("Ranged")]
        [SerializeField] private Transform bowMuzzle;
        [SerializeField] private Arrow arrowPrefab;
        [SerializeField] private float arrowSpeed = 28f;
        [SerializeField] private float rangedCooldown = 2.4f;
        [SerializeField] private float rangedRange = 24f;
        [SerializeField] private float inaccuracy = 0.06f;

        [Header("Melee")]
        [SerializeField] private float meleeRange = 1.8f;
        [SerializeField] private float meleeDamage = 15f;
        [SerializeField] private float meleeCooldown = 1.2f;
        [SerializeField] private LayerMask meleeMask = ~0;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip bowDrawClip;
        [SerializeField] private AudioClip bowReleaseClip;
        [SerializeField] private AudioClip meleeWhooshClip;

        [Header("Animation")]
        [SerializeField] private Animator animator;

        private float _rangedTimer;
        private float _meleeTimer;
        private Transform _pendingTarget;

        public bool CanRanged() => _rangedTimer <= 0f;
        public bool CanMelee() => _meleeTimer <= 0f;
        public float RangedRange => rangedRange;
        public float MeleeRange => meleeRange;

        void Update()
        {
            _rangedTimer = Mathf.Max(0f, _rangedTimer - Time.deltaTime);
            _meleeTimer = Mathf.Max(0f, _meleeTimer - Time.deltaTime);
        }

        public void TryRangedShot(Transform target)
        {
            if (!CanRanged() || target == null) return;
            _rangedTimer = rangedCooldown;
            _pendingTarget = target;
            if (animator) animator.SetTrigger("BowRelease");
            if (bowDrawClip && audioSource) audioSource.PlayOneShot(bowDrawClip, 0.7f);
            // If there are no anim events set up, release immediately.
            Invoke(nameof(ReleaseArrow), 0.15f);
        }

        // Wire this to an animation event on the bow release frame for best feel.
        public void AnimEvent_Release() { CancelInvoke(nameof(ReleaseArrow)); ReleaseArrow(); }

        void ReleaseArrow()
        {
            if (_pendingTarget == null || arrowPrefab == null || bowMuzzle == null) return;
            Vector3 origin = bowMuzzle.position;
            Vector3 aim = _pendingTarget.position + Vector3.up * 1.2f;
            Vector3 dir = (aim - origin).normalized;
            dir += Random.insideUnitSphere * inaccuracy;
            dir.Normalize();

            var arrow = Instantiate(arrowPrefab, origin, Quaternion.LookRotation(dir));
            arrow.Fire(dir * arrowSpeed, gameObject);
            if (bowReleaseClip && audioSource) audioSource.PlayOneShot(bowReleaseClip, 1f);
            _pendingTarget = null;

            Core.EventBus.Publish(new Core.EventBus.NoiseEvent
            {
                position = origin,
                radius = 14f,
                intensity = 0.6f,
                source = gameObject
            });
        }

        public void TryMelee(Transform target)
        {
            if (!CanMelee() || target == null) return;
            _meleeTimer = meleeCooldown;
            if (animator) animator.SetTrigger("Melee");
            if (meleeWhooshClip && audioSource) audioSource.PlayOneShot(meleeWhooshClip);
            Invoke(nameof(ApplyMelee), 0.25f);
        }

        public void AnimEvent_Melee() { CancelInvoke(nameof(ApplyMelee)); ApplyMelee(); }

        void ApplyMelee()
        {
            Vector3 origin = transform.position + transform.forward * 0.8f + Vector3.up * 1.3f;
            var hits = Physics.OverlapSphere(origin, meleeRange * 0.5f, meleeMask, QueryTriggerInteraction.Ignore);
            foreach (var h in hits)
            {
                var health = h.GetComponentInParent<Damage.Health>();
                if (health == null) continue;
                if (health.gameObject == gameObject) continue;
                health.ApplyDamage(new Damage.DamageInfo
                {
                    baseDamage = meleeDamage,
                    bodyPart = Damage.BodyPart.Torso,
                    hitPoint = h.ClosestPoint(origin),
                    hitDirection = (h.transform.position - transform.position).normalized,
                    hitForce = transform.forward * 400f,
                    source = gameObject
                });
                break;
            }
        }
    }
}
