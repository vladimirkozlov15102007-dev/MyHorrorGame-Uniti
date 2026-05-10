using UnityEngine;

namespace OldAmberFactory.AI
{
    /// <summary>
    /// Hooks for procedural motion & IK on skeletons. Drives a small set of
    /// "hints" that a full Animator / Motion-Matching solution (e.g., Kinematica,
    /// Motion Matching for Unity, FinalIK) will read.
    ///
    /// NOTE: Real motion matching requires a pose database (not implementable as code only).
    /// This component exposes the API and blends ragdoll / physical animation impulses so
    /// you can plug in FinalIK or Animation Rigging easily.
    /// </summary>
    [DisallowMultipleComponent]
    public class ProceduralMotion : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Transform spine;
        [SerializeField] private Transform head;
        [SerializeField] private Transform lookTarget;
        [SerializeField] private float headTurnSpeed = 6f;
        [SerializeField] private float spineSway = 2.5f;
        [SerializeField] private float breathAmplitude = 0.01f;
        [SerializeField] private float breathFrequency = 1.3f;
        [SerializeField] private float idleNoise = 0.005f;

        [Header("Hit Reaction")]
        [SerializeField] private float hitStaggerDecay = 5f;

        private float _stagger;
        private float _staggerAngle;
        private Vector3 _originalHeadLocalEuler;

        void Awake()
        {
            if (head) _originalHeadLocalEuler = head.localEulerAngles;
        }

        public void ApplyHit(Vector3 direction, float strength)
        {
            _stagger = Mathf.Max(_stagger, strength);
            _staggerAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            if (animator) animator.SetTrigger("Hit");
        }

        public void SetLookTarget(Transform t) { lookTarget = t; }

        void LateUpdate()
        {
            if (spine)
            {
                // Subtle breathing + hit lean.
                float lean = Mathf.Sin(Time.time * breathFrequency) * breathAmplitude;
                float stg = _stagger;
                spine.localPosition += Vector3.up * lean;
                spine.localRotation *= Quaternion.Euler(stg * spineSway, 0f, Mathf.Sin(Time.time * 0.6f) * idleNoise * 40f);
            }
            if (head && lookTarget)
            {
                Vector3 dir = lookTarget.position - head.position;
                Quaternion look = Quaternion.LookRotation(dir, Vector3.up);
                head.rotation = Quaternion.Slerp(head.rotation, look, Time.deltaTime * headTurnSpeed);
            }
            _stagger = Mathf.Max(0f, _stagger - Time.deltaTime * hitStaggerDecay);
        }
    }
}
