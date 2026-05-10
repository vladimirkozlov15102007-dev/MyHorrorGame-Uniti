using UnityEngine;

namespace OldAmberFactory.AI
{
    /// <summary>
    /// Translates the recommended counter-tactic into blackboard flags for BT nodes to read.
    /// </summary>
    public class TacticsController : MonoBehaviour
    {
        [SerializeField] private float retargetInterval = 4f;
        private float _timer;

        public string CurrentTactic { get; private set; } = "default";

        public void Tick(SkeletonAgent agent)
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = retargetInterval;

            var analyzer = PlayerBehaviorAnalyzer.Instance;
            if (analyzer == null) { CurrentTactic = "default"; return; }

            CurrentTactic = analyzer.RecommendCounterTactic();
            agent.Blackboard.Set("tactic", CurrentTactic);
            // Hints so BT nodes can react.
            agent.Blackboard.Set("wantFlank", CurrentTactic == "flank");
            agent.Blackboard.Set("wantAmbush", CurrentTactic == "ambush");
            agent.Blackboard.Set("wantSuppress", CurrentTactic == "suppress");
            agent.Blackboard.Set("wantKeepDistance", CurrentTactic == "keep_distance");
            agent.Blackboard.Set("wantIntercept", CurrentTactic == "intercept");
        }
    }
}
