using System.Collections.Generic;
using UnityEngine;

namespace OldAmberFactory.AI
{
    public enum NodeStatus { Running, Success, Failure }

    public abstract class BTNode
    {
        public abstract NodeStatus Tick(AIContext ctx);
    }

    public class AIContext
    {
        public SkeletonAgent agent;
        public Blackboard blackboard;
        public float deltaTime;
    }

    // ---- Composite Nodes ----

    public class Sequence : BTNode
    {
        readonly List<BTNode> children;
        int _current;
        public Sequence(params BTNode[] nodes) { children = new List<BTNode>(nodes); }
        public override NodeStatus Tick(AIContext ctx)
        {
            while (_current < children.Count)
            {
                var s = children[_current].Tick(ctx);
                if (s == NodeStatus.Running) return NodeStatus.Running;
                if (s == NodeStatus.Failure) { _current = 0; return NodeStatus.Failure; }
                _current++;
            }
            _current = 0;
            return NodeStatus.Success;
        }
    }

    public class Selector : BTNode
    {
        readonly List<BTNode> children;
        int _current;
        public Selector(params BTNode[] nodes) { children = new List<BTNode>(nodes); }
        public override NodeStatus Tick(AIContext ctx)
        {
            while (_current < children.Count)
            {
                var s = children[_current].Tick(ctx);
                if (s == NodeStatus.Running) return NodeStatus.Running;
                if (s == NodeStatus.Success) { _current = 0; return NodeStatus.Success; }
                _current++;
            }
            _current = 0;
            return NodeStatus.Failure;
        }
    }

    public class Parallel : BTNode
    {
        readonly List<BTNode> children;
        readonly int successThreshold;
        public Parallel(int successThreshold, params BTNode[] nodes)
        { children = new List<BTNode>(nodes); this.successThreshold = successThreshold; }
        public override NodeStatus Tick(AIContext ctx)
        {
            int success = 0, failure = 0;
            foreach (var c in children)
            {
                var s = c.Tick(ctx);
                if (s == NodeStatus.Success) success++;
                else if (s == NodeStatus.Failure) failure++;
            }
            if (success >= successThreshold) return NodeStatus.Success;
            if (failure > children.Count - successThreshold) return NodeStatus.Failure;
            return NodeStatus.Running;
        }
    }

    public class Inverter : BTNode
    {
        readonly BTNode child;
        public Inverter(BTNode child) { this.child = child; }
        public override NodeStatus Tick(AIContext ctx)
        {
            var s = child.Tick(ctx);
            return s switch
            {
                NodeStatus.Success => NodeStatus.Failure,
                NodeStatus.Failure => NodeStatus.Success,
                _ => NodeStatus.Running
            };
        }
    }

    public class Condition : BTNode
    {
        readonly System.Func<AIContext, bool> predicate;
        public Condition(System.Func<AIContext, bool> predicate) { this.predicate = predicate; }
        public override NodeStatus Tick(AIContext ctx) => predicate(ctx) ? NodeStatus.Success : NodeStatus.Failure;
    }

    public class Action : BTNode
    {
        readonly System.Func<AIContext, NodeStatus> func;
        public Action(System.Func<AIContext, NodeStatus> func) { this.func = func; }
        public override NodeStatus Tick(AIContext ctx) => func(ctx);
    }
}
