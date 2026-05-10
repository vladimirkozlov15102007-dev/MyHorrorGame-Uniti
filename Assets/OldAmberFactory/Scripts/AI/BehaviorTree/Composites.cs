using System.Collections.Generic;

namespace OldAmberFactory.AI.BehaviorTree
{
    /// <summary>Runs children in order; returns Success as soon as one succeeds.</summary>
    public class Selector : BTNode
    {
        private readonly List<BTNode> _children;
        public Selector(params BTNode[] children) { _children = new List<BTNode>(children); }

        protected override void OnBind()
        {
            foreach (var c in _children) c.Bind(BB);
        }

        protected override BTStatus OnTick()
        {
            foreach (var c in _children)
            {
                var s = c.Tick();
                if (s == BTStatus.Running) return BTStatus.Running;
                if (s == BTStatus.Success) return BTStatus.Success;
            }
            return BTStatus.Failure;
        }
    }

    /// <summary>Runs children in order; fails as soon as one fails.</summary>
    public class Sequence : BTNode
    {
        private readonly List<BTNode> _children;
        public Sequence(params BTNode[] children) { _children = new List<BTNode>(children); }

        protected override void OnBind()
        {
            foreach (var c in _children) c.Bind(BB);
        }

        protected override BTStatus OnTick()
        {
            foreach (var c in _children)
            {
                var s = c.Tick();
                if (s == BTStatus.Running) return BTStatus.Running;
                if (s == BTStatus.Failure) return BTStatus.Failure;
            }
            return BTStatus.Success;
        }
    }

    /// <summary>Inverts the child result.</summary>
    public class Inverter : BTNode
    {
        private readonly BTNode _child;
        public Inverter(BTNode child) { _child = child; }
        protected override void OnBind() => _child.Bind(BB);
        protected override BTStatus OnTick()
        {
            var s = _child.Tick();
            return s == BTStatus.Success ? BTStatus.Failure :
                   s == BTStatus.Failure ? BTStatus.Success :
                                           BTStatus.Running;
        }
    }

    /// <summary>
    /// Utility-AI style composite: evaluates children via scoring callbacks and ticks only the highest.
    /// </summary>
    public class UtilitySelector : BTNode
    {
        public delegate float ScoreFn(Blackboard bb);
        private readonly List<(ScoreFn score, BTNode node)> _choices = new();

        public UtilitySelector Add(ScoreFn score, BTNode node)
        {
            _choices.Add((score, node));
            return this;
        }

        protected override void OnBind()
        {
            foreach (var (_, n) in _choices) n.Bind(BB);
        }

        protected override BTStatus OnTick()
        {
            float best = float.NegativeInfinity;
            BTNode bestNode = null;
            for (int i = 0; i < _choices.Count; i++)
            {
                float s = _choices[i].score(BB);
                if (s > best) { best = s; bestNode = _choices[i].node; }
            }
            return bestNode != null ? bestNode.Tick() : BTStatus.Failure;
        }
    }
}
