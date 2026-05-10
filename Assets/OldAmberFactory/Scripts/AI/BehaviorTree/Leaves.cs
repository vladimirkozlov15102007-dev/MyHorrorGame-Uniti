using System;

namespace OldAmberFactory.AI.BehaviorTree
{
    /// <summary>Runs an Action and returns Success.</summary>
    public class ActionLeaf : BTNode
    {
        private readonly Action _fn;
        public ActionLeaf(Action fn) { _fn = fn; }
        protected override BTStatus OnTick() { _fn?.Invoke(); return BTStatus.Success; }
    }

    /// <summary>Returns Success if the predicate is true, Failure otherwise.</summary>
    public class ConditionLeaf : BTNode
    {
        private readonly Func<Blackboard,bool> _fn;
        public ConditionLeaf(Func<Blackboard,bool> fn) { _fn = fn; }
        protected override BTStatus OnTick() => _fn(BB) ? BTStatus.Success : BTStatus.Failure;
    }

    /// <summary>Returns whatever the supplied function returns.</summary>
    public class TaskLeaf : BTNode
    {
        private readonly Func<Blackboard,BTStatus> _fn;
        public TaskLeaf(Func<Blackboard,BTStatus> fn) { _fn = fn; }
        protected override BTStatus OnTick() => _fn(BB);
    }
}
