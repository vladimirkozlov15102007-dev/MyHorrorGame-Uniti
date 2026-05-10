using OldAmberFactory.AI.BehaviorTree;
using OldAmberFactory.Audio;
using OldAmberFactory.Core;
using OldAmberFactory.Damage;
using UnityEngine;
using UnityEngine.AI;

namespace OldAmberFactory.AI
{
    /// <summary>
    /// Concrete skeleton enemy. Binds together:
    /// - HealthSystem with 100 HP (design spec).
    /// - NavMeshAgent for locomotion on the NavMesh.
    /// - Perception (sight + hearing).
    /// - Behaviour tree built in code (Selector -> states by priority).
    /// - Group coordinator registration.
    ///
    /// Uses Utility AI at the root to choose between Combat / Ambush / Investigate / Patrol based
    /// on blackboard state and assigned role.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent), typeof(HealthSystem))]
    public class SkeletonArcher : MonoBehaviour
    {
        [Header("Locomotion")]
        [SerializeField] private NavMeshAgent _agent;
        [SerializeField] private Animator _animator;
        [SerializeField] private RagdollController _ragdoll;

        [Header("Combat")]
        [SerializeField] private Transform _bowMuzzle;
        [SerializeField] private Arrow _arrowPrefab;
        [SerializeField] private float _arrowVelocity = 28f;
        [SerializeField] private float _fireInterval = 2.2f;
        [SerializeField] private float _preferredRange = 11f;
        [SerializeField] private float _meleeRange = 1.9f;
        [SerializeField] private float _meleeDamage = 18f;
        [SerializeField] private float _meleeCooldown = 1.5f;

        [Header("Tuning")]
        [SerializeField] private Transform[] _patrolRoute;
        [SerializeField] private float _investigateArrivalDistance = 1.2f;

        private HealthSystem _health;
        private Perception _perception;
        private Blackboard _bb;
        private BTNode _root;

        private float _nextFireAt;
        private float _nextMeleeAt;
        private int   _patrolIndex;

        public Blackboard BB => _bb;
        public HealthSystem Health => _health;

        private void Awake()
        {
            _health = GetComponent<HealthSystem>();
            _perception = GetComponent<Perception>();
            if (_agent == null) _agent = GetComponent<NavMeshAgent>();

            _bb = new Blackboard();
            _perception?.Bind(_bb);

            _health.OnDamaged += OnDamaged;
            _health.OnDied    += OnDied;

            BuildBehaviorTree();
        }

        private void OnEnable()
        {
            var coord = ServiceLocator.Get<GroupAICoordinator>();
            coord?.RegisterAgent(this);
        }

        private void OnDisable()
        {
            var coord = ServiceLocator.Get<GroupAICoordinator>();
            coord?.UnregisterAgent(this);
        }

        private void Update()
        {
            if (_health.IsDead) return;

            _bb.Set(BBKeys.HealthNorm, _health.Normalized);
            _root?.Tick();

            if (_animator != null)
            {
                _animator.SetFloat("Speed", _agent.velocity.magnitude);
                _animator.SetBool("Crouched", _bb.Get<bool>("crouched"));
            }
        }

        // ─── Behaviour tree construction ────────────────────────────────────────────

        private void BuildBehaviorTree()
        {
            var combat = new Sequence(
                new ConditionLeaf(bb => bb.Get<bool>(BBKeys.SawPlayer)
                                     || bb.Get<float>(BBKeys.TimeSinceSeen, 999f) < 4f),
                new UtilitySelector()
                    .Add(ScoreMelee,      new TaskLeaf(DoMelee))
                    .Add(ScoreFlank,      new TaskLeaf(DoFlankAdvance))
                    .Add(ScoreSuppress,   new TaskLeaf(DoSuppressiveFire))
                    .Add(ScoreAssault,    new TaskLeaf(DoAssault))
                    .Add(ScoreRetreat,    new TaskLeaf(DoRetreatToCover))
            );

            var investigate = new Sequence(
                new ConditionLeaf(bb => bb.Has(BBKeys.HeardNoisePos)
                                     && bb.Get<float>(BBKeys.TimeSinceHeard, 999f) < 10f),
                new TaskLeaf(DoInvestigate)
            );

            var ambush = new Sequence(
                new ConditionLeaf(bb => bb.Get<GroupRole>(BBKeys.AssignedRole) == GroupRole.Ambusher),
                new TaskLeaf(DoAmbush)
            );

            var patrol = new TaskLeaf(DoPatrol);

            _root = new Selector(combat, ambush, investigate, patrol);
            _root.Bind(_bb);
        }

        // ─── Utility scores ─────────────────────────────────────────────────────────

        private float ScoreMelee(Blackboard bb)
        {
            var pl = bb.Get<Transform>(BBKeys.PlayerRef);
            if (pl == null) return 0f;
            float d = Vector3.Distance(transform.position, pl.position);
            return d < _meleeRange ? 100f : 0f;
        }
        private float ScoreFlank(Blackboard bb)
        {
            var role = bb.Get<GroupRole>(BBKeys.AssignedRole);
            return (role == GroupRole.FlankLeft || role == GroupRole.FlankRight) ? 70f : 0f;
        }
        private float ScoreSuppress(Blackboard bb)
        {
            return bb.Get<GroupRole>(BBKeys.AssignedRole) == GroupRole.Suppressor ? 65f : 0f;
        }
        private float ScoreAssault(Blackboard bb)
        {
            return bb.Get<GroupRole>(BBKeys.AssignedRole) == GroupRole.Assault ? 55f : 25f;
        }
        private float ScoreRetreat(Blackboard bb)
        {
            float hn = bb.Get(BBKeys.HealthNorm, 1f);
            return hn < 0.35f ? 90f : 0f;
        }

        // ─── Tasks ──────────────────────────────────────────────────────────────────

        private BTStatus DoPatrol(Blackboard bb)
        {
            if (_patrolRoute == null || _patrolRoute.Length == 0) return BTStatus.Failure;
            var target = _patrolRoute[_patrolIndex];
            if (target == null) return BTStatus.Failure;
            _agent.SetDestination(target.position);
            if (!_agent.pathPending && _agent.remainingDistance < 1.2f)
                _patrolIndex = (_patrolIndex + 1) % _patrolRoute.Length;
            return BTStatus.Running;
        }

        private BTStatus DoInvestigate(Blackboard bb)
        {
            Vector3 pos = bb.Get<Vector3>(BBKeys.HeardNoisePos);
            _agent.SetDestination(pos);
            if (!_agent.pathPending && _agent.remainingDistance < _investigateArrivalDistance)
            {
                bb.Clear(BBKeys.HeardNoisePos);
                return BTStatus.Success;
            }
            return BTStatus.Running;
        }

        private BTStatus DoAmbush(Blackboard bb)
        {
            if (!bb.TryGet<Vector3>(BBKeys.AmbushPoint, out var amb))
            {
                if (bb.TryGet<Vector3>(BBKeys.LastKnownPos, out var lk))
                {
                    // pick a cover between us and the last-known player pos
                    var cover = CoverRegistry.QueryNearestAvailable(transform.position, lk, 20f);
                    if (cover != null && cover.TryReserve())
                    {
                        bb.Set(BBKeys.AmbushPoint, cover.transform.position);
                        amb = cover.transform.position;
                    }
                    else return BTStatus.Failure;
                }
                else return BTStatus.Failure;
            }

            _agent.SetDestination(amb);
            if (!_agent.pathPending && _agent.remainingDistance < 1f)
            {
                // crouch, wait, fire opportunistically
                bb.Set("crouched", true);
                if (bb.Get<bool>(BBKeys.SawPlayer)) TryFireArrow(bb);
            }
            return BTStatus.Running;
        }

        private BTStatus DoAssault(Blackboard bb)
        {
            bb.Set("crouched", false);
            var pl = bb.Get<Transform>(BBKeys.PlayerRef);
            Vector3 t = pl != null ? pl.position : bb.Get<Vector3>(BBKeys.LastKnownPos);
            Vector3 to = t - transform.position;
            float dist = to.magnitude;

            // keep preferred range
            Vector3 dest = t - to.normalized * _preferredRange;
            _agent.SetDestination(dest);

            if (bb.Get<bool>(BBKeys.SawPlayer) && dist < _preferredRange * 1.5f)
                TryFireArrow(bb);
            return BTStatus.Running;
        }

        private BTStatus DoFlankAdvance(Blackboard bb)
        {
            bb.Set("crouched", false);
            int dir = bb.Get<int>(BBKeys.FlankDir, 1);
            if (!bb.TryGet<Vector3>(BBKeys.LastKnownPos, out var lk)) return BTStatus.Failure;

            Vector3 lateral = Vector3.Cross(Vector3.up, (lk - transform.position).normalized) * dir;
            Vector3 dest = lk + lateral * 6f;
            if (NavMesh.SamplePosition(dest, out var nh, 4f, NavMesh.AllAreas))
                _agent.SetDestination(nh.position);

            if (bb.Get<bool>(BBKeys.SawPlayer)) TryFireArrow(bb);
            return BTStatus.Running;
        }

        private BTStatus DoSuppressiveFire(Blackboard bb)
        {
            // Stand off and fire at the suppress target (last-known pos) at a faster cadence.
            var target = bb.Get<Vector3>(BBKeys.SuppressTarget);
            Vector3 to = target - transform.position;
            Vector3 dest = target - to.normalized * Mathf.Max(_preferredRange, 9f);
            _agent.SetDestination(dest);

            if (Time.time > _nextFireAt)
            {
                FireArrowAt(target + Random.insideUnitSphere * 1.2f);
                _nextFireAt = Time.time + _fireInterval * 0.7f;
            }
            return BTStatus.Running;
        }

        private BTStatus DoRetreatToCover(Blackboard bb)
        {
            if (!bb.TryGet<Vector3>(BBKeys.LastKnownPos, out var threat)) return BTStatus.Failure;
            var c = CoverRegistry.QueryNearestAvailable(transform.position, threat, 30f);
            if (c == null) return BTStatus.Failure;
            if (!c.TryReserve()) return BTStatus.Failure;

            _agent.SetDestination(c.transform.position);
            return BTStatus.Running;
        }

        private BTStatus DoMelee(Blackboard bb)
        {
            var pl = bb.Get<Transform>(BBKeys.PlayerRef);
            if (pl == null) return BTStatus.Failure;
            Vector3 to = pl.position - transform.position;
            if (to.sqrMagnitude > _meleeRange * _meleeRange) { _agent.SetDestination(pl.position); return BTStatus.Running; }

            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(new Vector3(to.x, 0f, to.z)), 10f * Time.deltaTime);

            if (Time.time >= _nextMeleeAt)
            {
                _nextMeleeAt = Time.time + _meleeCooldown;
                _animator?.SetTrigger("Melee");

                // sample a receiver on the player
                if (pl.TryGetComponent<HealthSystem>(out var hs))
                {
                    hs.ReceiveDamage(new DamageInfo
                    {
                        amount = _meleeDamage,
                        bodyPart = BodyPartType.Torso,
                        hitDirection = to.normalized,
                        hitPoint = pl.position,
                        impulse = 0f,
                        instigator = gameObject
                    });
                }
            }
            return BTStatus.Running;
        }

        private void TryFireArrow(Blackboard bb)
        {
            if (Time.time < _nextFireAt) return;
            var pl = bb.Get<Transform>(BBKeys.PlayerRef);
            if (pl == null) return;

            Vector3 target = pl.position + Vector3.up * 1.0f;
            // lead the target slightly if player is moving
            if (pl.TryGetComponent<CharacterController>(out var cc))
                target += cc.velocity * 0.15f;

            FireArrowAt(target);
            _nextFireAt = Time.time + _fireInterval;
        }

        private void FireArrowAt(Vector3 world)
        {
            if (_arrowPrefab == null || _bowMuzzle == null) return;
            Vector3 dir = (world - _bowMuzzle.position).normalized;
            var a = Instantiate(_arrowPrefab, _bowMuzzle.position, Quaternion.LookRotation(dir));
            a.Launch(dir * _arrowVelocity, gameObject);
            _animator?.SetTrigger("Shoot");
            NoiseEventBus.Broadcast(transform.position, 8f, NoiseSource.Voice, gameObject);
        }

        // ─── Damage reactions ───────────────────────────────────────────────────────

        private void OnDamaged(DamageInfo info)
        {
            _animator?.SetTrigger("Hit");
            // reporting the damage source to the group
            var coord = ServiceLocator.Get<GroupAICoordinator>();
            if (info.instigator != null) coord?.ReportPlayer(info.instigator.transform.position);

            _bb.Set(BBKeys.LastKnownPos, info.instigator != null ? info.instigator.transform.position : transform.position);
            _bb.Set(BBKeys.TimeSinceSeen, 0f);
        }

        private void OnDied(DamageInfo info)
        {
            _agent.enabled = false;
            if (_ragdoll != null)
                _ragdoll.SetRagdoll(true, info.hitPoint, info.hitDirection, info.impulse * 10f);
            var coord = ServiceLocator.Get<GroupAICoordinator>();
            coord?.UnregisterAgent(this);
            Destroy(this); // disable AI script; ragdoll keeps rendering
        }
    }
}
