using UnityEngine;
using UnityEngine.AI;
using OldAmberFactory.Damage;

namespace OldAmberFactory.AI
{
    /// <summary>
    /// Top-level skeleton archer controller. Composes:
    ///   - NavMeshAgent (locomotion)
    ///   - Perception (sight + hearing)
    ///   - SkeletonCombat (bow + melee)
    ///   - ProceduralMotion (IK/physical anim hooks)
    ///   - TacticsController (adaptive tactic)
    ///   - Behavior Tree (state logic)
    /// Registers with SquadCoordinator for group behavior.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Health))]
    public class SkeletonAgent : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Perception perception;
        [SerializeField] private SkeletonCombat combat;
        [SerializeField] private ProceduralMotion procMotion;
        [SerializeField] private CoverFinder coverFinder;
        [SerializeField] private TacticsController tactics;
        [SerializeField] private RagdollController ragdoll;
        [SerializeField] private Animator animator;

        [Header("Patrol")]
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField] private float waypointTolerance = 0.6f;

        [Header("Behavior Tuning")]
        [SerializeField] private float combatKeepDistance = 9f;
        [SerializeField] private float combatRetreatHealth = 0.25f;
        [SerializeField] private float investigateDuration = 6f;
        [SerializeField] private float searchDuration = 12f;

        public NavMeshAgent Agent { get; private set; }
        public Health Health { get; private set; }
        public Perception Perception => perception;
        public SkeletonCombat Combat => combat;
        public ProceduralMotion Motion => procMotion;
        public CoverFinder CoverFinder_ => coverFinder;
        public Animator Animator => animator;
        public Blackboard Blackboard { get; } = new Blackboard();
        public Transform[] PatrolPoints => patrolPoints;
        public float WaypointTolerance => waypointTolerance;
        public float CombatKeepDistance => combatKeepDistance;
        public float CombatRetreatHealth => combatRetreatHealth;
        public float InvestigateDuration => investigateDuration;
        public float SearchDuration => searchDuration;
        public bool IsDead => Health != null && Health.IsDead;

        private BTNode _root;
        private AIContext _ctx;

        void Awake()
        {
            Agent = GetComponent<NavMeshAgent>();
            Health = GetComponent<Health>();
            if (perception == null) perception = GetComponent<Perception>();
            if (combat == null) combat = GetComponent<SkeletonCombat>();
            if (procMotion == null) procMotion = GetComponent<ProceduralMotion>();
            if (coverFinder == null) coverFinder = GetComponent<CoverFinder>();
            if (tactics == null) tactics = GetComponent<TacticsController>();
            if (ragdoll == null) ragdoll = GetComponent<RagdollController>();

            _ctx = new AIContext { agent = this, blackboard = Blackboard };
            _root = SkeletonBehaviorTreeFactory.Build(this);
        }

        void OnEnable()
        {
            Health.OnDied += HandleDied;
            Health.OnDamaged += HandleDamaged;
            SquadCoordinator.Instance?.Register(this);
            Core.GameManager.Instance?.RegisterSkeleton(gameObject);
        }

        void OnDisable()
        {
            if (Health != null)
            {
                Health.OnDied -= HandleDied;
                Health.OnDamaged -= HandleDamaged;
            }
            SquadCoordinator.Instance?.Unregister(this);
        }

        void Start()
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                Blackboard.Set("player", player.transform);
                if (procMotion) procMotion.SetLookTarget(player.transform);
            }
        }

        void Update()
        {
            if (IsDead) return;
            var playerT = Blackboard.Get<Transform>("player");
            if (perception) perception.Tick(playerT);
            if (tactics) tactics.Tick(this);

            // Push current knowledge into the blackboard.
            if (perception && perception.HasLineOfSight)
            {
                Blackboard.Set("lastKnownPos", perception.LastKnownPosition);
                Blackboard.Set("lastSeenTime", perception.LastSeenTime);
                SquadCoordinator.Instance?.ReportContact(this, perception.LastKnownPosition);
            }
            else if (perception && perception.HasRecentKnowledge())
            {
                Blackboard.Set("lastKnownPos", perception.LastKnownPosition);
            }

            _ctx.deltaTime = Time.deltaTime;
            _root.Tick(_ctx);

            UpdateAnimator();
        }

        void UpdateAnimator()
        {
            if (animator == null) return;
            animator.SetFloat("Speed", Agent.velocity.magnitude);
            animator.SetBool("InCombat", Blackboard.Get("isInCombat", false));
        }

        void HandleDamaged(DamageInfo info)
        {
            // React to a hit: stagger + alert.
            if (procMotion) procMotion.ApplyHit(info.hitDirection, Mathf.Clamp01(info.FinalDamage() / 30f));
            Blackboard.Set("isInCombat", true);
            Blackboard.Set("lastKnownPos", info.source ? info.source.transform.position : transform.position);
            Blackboard.Set("lastSeenTime", Time.time);
            SquadCoordinator.Instance?.ReportContact(this, info.source ? info.source.transform.position : transform.position);
        }

        void HandleDied(DamageInfo info)
        {
            if (animator) animator.enabled = false;
            if (Agent && Agent.enabled) { Agent.isStopped = true; Agent.enabled = false; }
            if (ragdoll)
                ragdoll.Activate(info.hitForce, info.hitPoint);
            Core.GameManager.Instance?.ReportSkeletonDeath(gameObject);
            SquadCoordinator.Instance?.Unregister(this);
            // Disable self-behaviors.
            this.enabled = false;
        }
    }
}
