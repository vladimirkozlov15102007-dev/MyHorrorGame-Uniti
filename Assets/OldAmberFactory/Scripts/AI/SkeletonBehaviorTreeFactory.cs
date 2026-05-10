using UnityEngine;
using UnityEngine.AI;

namespace OldAmberFactory.AI
{
    /// <summary>
    /// Builds the Behavior Tree for a SkeletonAgent.
    /// Priority (selector):
    ///   1) Dead? -> do nothing (handled elsewhere)
    ///   2) Combat: if sees player OR squad has contact -> engage (with tactic variants)
    ///   3) Alert / Investigate: has recent knowledge -> move to last known pos, search
    ///   4) Patrol: default idle-walk between waypoints
    /// </summary>
    public static class SkeletonBehaviorTreeFactory
    {
        public static BTNode Build(SkeletonAgent a)
        {
            var combat = BuildCombat(a);
            var alert = BuildAlert(a);
            var patrol = BuildPatrol(a);

            return new Selector(combat, alert, patrol);
        }

        // ---------- COMBAT ----------
        static BTNode BuildCombat(SkeletonAgent a)
        {
            // Enter combat if we see the player OR squad shares contact.
            var hasContact = new Condition(ctx =>
            {
                var bb = ctx.blackboard;
                return (a.Perception != null && a.Perception.HasLineOfSight) ||
                       bb.Get("sharedContact", false);
            });

            var setInCombat = new Action(ctx => { ctx.blackboard.Set("isInCombat", true); return NodeStatus.Success; });

            // Retreat when low health.
            var retreatIfLow = new Sequence(
                new Condition(ctx => a.Health.Normalized < a.CombatRetreatHealth),
                BTActions.MoveToCover(a, urgent: true)
            );

            // Tactic-based combat:
            var tacticSelector = new Selector(
                // Flank
                new Sequence(
                    new Condition(ctx => ctx.blackboard.Get<string>("role") == "flanker" || ctx.blackboard.Get("wantFlank", false)),
                    BTActions.MoveToFlank(a),
                    BTActions.ShootIfPossible(a)
                ),
                // Ambush
                new Sequence(
                    new Condition(ctx => ctx.blackboard.Get("wantAmbush", false)),
                    BTActions.MoveToAmbush(a),
                    BTActions.WaitAndStrike(a)
                ),
                // Suppress
                new Sequence(
                    new Condition(ctx => ctx.blackboard.Get<string>("role") == "suppressor" || ctx.blackboard.Get("wantSuppress", false)),
                    BTActions.StayAtRange(a),
                    BTActions.ShootIfPossible(a, suppressive: true)
                ),
                // Keep distance (player is aggressive)
                new Sequence(
                    new Condition(ctx => ctx.blackboard.Get("wantKeepDistance", false)),
                    BTActions.KeepDistance(a),
                    BTActions.ShootIfPossible(a)
                ),
                // Intercept (player is mobile)
                new Sequence(
                    new Condition(ctx => ctx.blackboard.Get("wantIntercept", false)),
                    BTActions.Intercept(a),
                    BTActions.ShootIfPossible(a)
                ),
                // Default: attacker - close to melee/shoot range and fire.
                new Sequence(
                    BTActions.Approach(a),
                    new Selector(
                        BTActions.MeleeIfPossible(a),
                        BTActions.ShootIfPossible(a)
                    )
                )
            );

            return new Sequence(hasContact, setInCombat, new Selector(retreatIfLow, tacticSelector));
        }

        // ---------- ALERT / INVESTIGATE / SEARCH ----------
        static BTNode BuildAlert(SkeletonAgent a)
        {
            var hasKnowledge = new Condition(ctx =>
                a.Perception != null && a.Perception.HasRecentKnowledge());

            var setAlert = new Action(ctx => { ctx.blackboard.Set("isInCombat", false); return NodeStatus.Success; });

            return new Sequence(
                hasKnowledge,
                setAlert,
                BTActions.Investigate(a),
                BTActions.Search(a)
            );
        }

        // ---------- PATROL ----------
        static BTNode BuildPatrol(SkeletonAgent a)
        {
            return BTActions.Patrol(a);
        }
    }
}
