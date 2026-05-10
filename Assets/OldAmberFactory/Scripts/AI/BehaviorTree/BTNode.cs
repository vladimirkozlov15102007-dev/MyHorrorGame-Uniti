namespace OldAmberFactory.AI.BehaviorTree
{
    public enum BTStatus
    {
        Success,
        Failure,
        Running
    }

    /// <summary>
    /// Minimal synchronous behaviour-tree node. Ticked each frame by the owner agent.
    /// Composites (Selector/Sequence) and decorators live in separate files.
    /// </summary>
    public abstract class BTNode
    {
        public Blackboard BB { get; private set; }

        public void Bind(Blackboard bb)
        {
            BB = bb;
            OnBind();
        }
        protected virtual void OnBind() {}

        public BTStatus Tick()
        {
            return OnTick();
        }

        protected abstract BTStatus OnTick();
    }
}
