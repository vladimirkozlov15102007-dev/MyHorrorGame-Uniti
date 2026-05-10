using UnityEngine;
using OldAmberFactory.AI;

namespace OldAmberFactory.Audio
{
    /// <summary>
    /// Drives AdaptiveMusic based on:
    ///  - nearest enemy distance
    ///  - number of engaged enemies
    ///  - player HP (critical)
    /// </summary>
    public class MusicDirector : MonoBehaviour
    {
        [SerializeField] private AdaptiveMusic music;
        [SerializeField] private Transform player;
        [SerializeField] private Damage.Health playerHealth;
        [SerializeField] private float detectDistance = 15f;
        [SerializeField] private float tensionDistance = 28f;
        [SerializeField] private float criticalHealthNormalized = 0.3f;
        [SerializeField] private float evaluateInterval = 0.4f;

        float _timer;

        void Update()
        {
            _timer += Time.deltaTime;
            if (_timer < evaluateInterval) return;
            _timer = 0f;
            if (!music || !player) return;

            bool critical = playerHealth && playerHealth.Normalized < criticalHealthNormalized;

            float closest = float.MaxValue;
            int engaged = 0;
            foreach (var s in FindObjectsByType<SkeletonAgent>(FindObjectsSortMode.None))
            {
                if (s == null || s.IsDead) continue;
                float d = Vector3.Distance(player.position, s.transform.position);
                if (d < closest) closest = d;
                if (s.Blackboard.Get("isInCombat", false)) engaged++;
            }

            AdaptiveMusic.Mood mood = AdaptiveMusic.Mood.Calm;
            if (engaged >= 2) mood = AdaptiveMusic.Mood.Combat;
            else if (engaged == 1 || closest < detectDistance) mood = AdaptiveMusic.Mood.Detect;
            else if (closest < tensionDistance) mood = AdaptiveMusic.Mood.Tension;

            if (critical && (mood == AdaptiveMusic.Mood.Combat || mood == AdaptiveMusic.Mood.Detect))
                mood = AdaptiveMusic.Mood.Critical;

            music.SetMood(mood);
        }
    }
}
