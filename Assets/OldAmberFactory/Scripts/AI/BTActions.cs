using UnityEngine;
using UnityEngine.AI;

namespace OldAmberFactory.AI
{
    /// <summary>
    /// Reusable BT action factories used by SkeletonBehaviorTreeFactory.
    /// Nodes are lightweight and re-entrant; they store transient data on the blackboard.
    /// </summary>
    public static class BTActions
    {
        // ---------- PATROL ----------
        public static BTNode Patrol(SkeletonAgent a)
        {
            int idx = 0;
            return new Action(ctx =>
            {
                if (a.PatrolPoints == null || a.PatrolPoints.Length == 0)
                {
                    // Wander slightly around current pos.
                    if (!a.Agent.hasPath || a.Agent.remainingDistance < a.WaypointTolerance)
                    {
                        var dst = a.transform.position + Random.insideUnitSphere * 3f;
                        dst.y = a.transform.position.y;
                        if (NavMesh.SamplePosition(dst, out var hit, 2f, NavMesh.AllAreas))
                            a.Agent.SetDestination(hit.position);
                    }
                    a.Agent.speed = 1.1f;
                    return NodeStatus.Running;
                }

                var target = a.PatrolPoints[idx];
                if (target == null) { idx = (idx + 1) % a.PatrolPoints.Length; return NodeStatus.Running; }

                a.Agent.speed = 1.4f;
                a.Agent.SetDestination(target.position);
                if (!a.Agent.pathPending && a.Agent.remainingDistance < a.WaypointTolerance)
                    idx = (idx + 1) % a.PatrolPoints.Length;
                return NodeStatus.Running;
            });
        }

        // ---------- INVESTIGATE ----------
        public static BTNode Investigate(SkeletonAgent a)
        {
            float endTime = 0f;
            return new Action(ctx =>
            {
                var pos = ctx.blackboard.Get<Vector3>("lastKnownPos");
                if (endTime <= 0f) endTime = Time.time + a.InvestigateDuration;
                a.Agent.speed = 2.8f;
                a.Agent.SetDestination(pos);
                if (!a.Agent.pathPending && a.Agent.remainingDistance < 1.1f)
                {
                    endTime = 0f;
                    return NodeStatus.Success;
                }
                if (Time.time > endTime) { endTime = 0f; return NodeStatus.Success; }
                return NodeStatus.Running;
            });
        }

        // ---------- SEARCH ----------
        public static BTNode Search(SkeletonAgent a)
        {
            float endTime = 0f;
            float nextRepath = 0f;
            return new Action(ctx =>
            {
                var pos = ctx.blackboard.Get<Vector3>("lastKnownPos");
                if (endTime <= 0f) endTime = Time.time + a.SearchDuration;
                a.Agent.speed = 2.4f;
                if (Time.time > nextRepath)
                {
                    Vector3 p = pos + Random.insideUnitSphere * 5f;
                    if (NavMesh.SamplePosition(p, out var hit, 2.5f, NavMesh.AllAreas))
                        a.Agent.SetDestination(hit.position);
                    nextRepath = Time.time + 2.5f;
                }
                if (Time.time > endTime)
                {
                    endTime = 0f;
                    a.Blackboard.Set("sharedContact", false);
                    return NodeStatus.Success;
                }
                return NodeStatus.Running;
            });
        }

        // ---------- APPROACH ----------
        public static BTNode Approach(SkeletonAgent a)
        {
            return new Action(ctx =>
            {
                var target = ctx.blackboard.Get<Transform>("player");
                if (target == null) return NodeStatus.Failure;
                float ideal = a.Combat ? a.Combat.RangedRange * 0.8f : 10f;
                float d = Vector3.Distance(a.transform.position, target.position);
                a.Agent.speed = 3.6f;
                if (d > ideal) { a.Agent.SetDestination(target.position); return NodeStatus.Running; }
                return NodeStatus.Success;
            });
        }

        // ---------- KEEP DISTANCE ----------
        public static BTNode KeepDistance(SkeletonAgent a)
        {
            return new Action(ctx =>
            {
                var target = ctx.blackboard.Get<Transform>("player");
                if (target == null) return NodeStatus.Failure;
                float desired = a.CombatKeepDistance;
                Vector3 away = (a.transform.position - target.position).normalized;
                Vector3 dst = target.position + away * desired;
                if (NavMesh.SamplePosition(dst, out var hit, 2f, NavMesh.AllAreas))
                    a.Agent.SetDestination(hit.position);
                a.Agent.speed = 3.2f;
                return NodeStatus.Success;
            });
        }

        // ---------- STAY AT RANGE ----------
        public static BTNode StayAtRange(SkeletonAgent a)
        {
            return new Action(ctx =>
            {
                var target = ctx.blackboard.Get<Transform>("player");
                if (target == null) return NodeStatus.Failure;
                float d = Vector3.Distance(a.transform.position, target.position);
                float desired = a.Combat != null ? a.Combat.RangedRange * 0.9f : 18f;
                if (d < desired - 2f)
                {
                    Vector3 away = (a.transform.position - target.position).normalized;
                    Vector3 dst = target.position + away * desired;
                    if (NavMesh.SamplePosition(dst, out var hit, 2f, NavMesh.AllAreas))
                        a.Agent.SetDestination(hit.position);
                    a.Agent.speed = 2.6f;
                }
                else
                {
                    a.Agent.ResetPath();
                }
                return NodeStatus.Success;
            });
        }

        // ---------- INTERCEPT ----------
        public static BTNode Intercept(SkeletonAgent a)
        {
            return new Action(ctx =>
            {
                var target = ctx.blackboard.Get<Transform>("player");
                if (target == null) return NodeStatus.Failure;
                var pc = target.GetComponent<OldAmberFactory.Player.PlayerController>();
                Vector3 predicted = target.position;
                if (pc != null)
                {
                    var cc = target.GetComponent<CharacterController>();
                    if (cc != null) predicted = target.position + new Vector3(cc.velocity.x, 0f, cc.velocity.z) * 1.5f;
                }
                if (NavMesh.SamplePosition(predicted, out var hit, 2.5f, NavMesh.AllAreas))
                    a.Agent.SetDestination(hit.position);
                a.Agent.speed = 4.2f;
                return NodeStatus.Success;
            });
        }

        // ---------- MOVE TO COVER ----------
        public static BTNode MoveToCover(SkeletonAgent a, bool urgent = false)
        {
            return new Action(ctx =>
            {
                var target = ctx.blackboard.Get<Transform>("player");
                if (target == null || a.CoverFinder_ == null) return NodeStatus.Failure;
                if (!a.CoverFinder_.FindCover(a.transform.position, target.position, out var c))
                    return NodeStatus.Failure;
                a.Agent.speed = urgent ? 4.5f : 3.2f;
                a.Agent.SetDestination(c);
                ctx.blackboard.Set("coverPoint", c);
                return NodeStatus.Success;
            });
        }

        // ---------- MOVE TO FLANK ----------
        public static BTNode MoveToFlank(SkeletonAgent a)
        {
            return new Action(ctx =>
            {
                var target = ctx.blackboard.Get<Transform>("player");
                if (target == null || a.CoverFinder_ == null) return NodeStatus.Failure;
                if (!a.CoverFinder_.FindFlank(a.transform.position, target.position, target.forward, out var f))
                    return NodeStatus.Failure;
                a.Agent.speed = 4f;
                a.Agent.SetDestination(f);
                ctx.blackboard.Set("flankPoint", f);
                return NodeStatus.Success;
            });
        }

        // ---------- MOVE TO AMBUSH ----------
        public static BTNode MoveToAmbush(SkeletonAgent a)
        {
            return new Action(ctx =>
            {
                var target = ctx.blackboard.Get<Transform>("player");
                if (target == null || a.CoverFinder_ == null) return NodeStatus.Failure;
                if (!a.CoverFinder_.FindAmbush(target.position, out var p)) return NodeStatus.Failure;
                a.Agent.speed = 3.6f;
                a.Agent.SetDestination(p);
                ctx.blackboard.Set("ambushPoint", p);
                return NodeStatus.Success;
            });
        }

        // ---------- WAIT AND STRIKE ----------
        public static BTNode WaitAndStrike(SkeletonAgent a)
        {
            return new Action(ctx =>
            {
                var target = ctx.blackboard.Get<Transform>("player");
                if (target == null) return NodeStatus.Failure;
                a.Agent.ResetPath();
                float d = Vector3.Distance(a.transform.position, target.position);
                if (d < a.Combat.RangedRange * 0.6f && a.Perception.HasLineOfSight && a.Combat.CanRanged())
                {
                    a.Combat.TryRangedShot(target);
                    return NodeStatus.Success;
                }
                return NodeStatus.Running;
            });
        }

        // ---------- SHOOT ----------
        public static BTNode ShootIfPossible(SkeletonAgent a, bool suppressive = false)
        {
            return new Action(ctx =>
            {
                var target = ctx.blackboard.Get<Transform>("player");
                if (target == null || a.Combat == null) return NodeStatus.Failure;
                if (!a.Perception.HasLineOfSight && !suppressive) return NodeStatus.Failure;
                float d = Vector3.Distance(a.transform.position, target.position);
                if (d > a.Combat.RangedRange) return NodeStatus.Failure;
                // Face target.
                Vector3 look = target.position - a.transform.position;
                look.y = 0f;
                if (look.sqrMagnitude > 0.01f)
                    a.transform.rotation = Quaternion.Slerp(a.transform.rotation,
                        Quaternion.LookRotation(look), Time.deltaTime * 6f);
                if (a.Combat.CanRanged())
                {
                    a.Combat.TryRangedShot(target);
                    return NodeStatus.Success;
                }
                return NodeStatus.Running;
            });
        }

        // ---------- MELEE ----------
        public static BTNode MeleeIfPossible(SkeletonAgent a)
        {
            return new Action(ctx =>
            {
                var target = ctx.blackboard.Get<Transform>("player");
                if (target == null || a.Combat == null) return NodeStatus.Failure;
                float d = Vector3.Distance(a.transform.position, target.position);
                if (d > a.Combat.MeleeRange) return NodeStatus.Failure;
                if (a.Combat.CanMelee()) { a.Combat.TryMelee(target); return NodeStatus.Success; }
                return NodeStatus.Running;
            });
        }
    }
}
