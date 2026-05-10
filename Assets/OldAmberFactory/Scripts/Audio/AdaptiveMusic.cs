using OldAmberFactory.AI;
using OldAmberFactory.Core;
using UnityEngine;

namespace OldAmberFactory.Audio
{
    /// <summary>
    /// Layered adaptive score driver. Five looping stems all play in sync but at
    /// different volumes. Engine picks a target "intensity" from AI state and cross-fades.
    /// </summary>
    public class AdaptiveMusic : MonoBehaviour
    {
        public enum Intensity
        {
            Calm     = 0,
            Tension  = 1,
            Detected = 2,
            Combat   = 3,
            Critical = 4
        }

        [System.Serializable]
        public class Layer
        {
            public Intensity intensity;
            public AudioSource source;
            [Range(0f, 1f)] public float targetVolume = 1f;
        }

        [SerializeField] private Layer[] _layers;
        [SerializeField] private Transform _player;
        [SerializeField] private float _fadeSpeed = 0.8f;
        [SerializeField] private float _detectionRange = 18f;
        [SerializeField] private float _combatRange = 12f;

        private Intensity _target = Intensity.Calm;
        private float _nextEvalAt;
        private const float kEvalInterval = 0.25f;

        private void Start()
        {
            foreach (var l in _layers)
            {
                if (l.source == null) continue;
                l.source.volume = 0f;
                l.source.loop = true;
                if (!l.source.isPlaying) l.source.Play();
            }
        }

        private void Update()
        {
            if (Time.time >= _nextEvalAt)
            {
                _target = EvaluateIntensity();
                _nextEvalAt = Time.time + kEvalInterval;
            }
            for (int i = 0; i < _layers.Length; i++)
            {
                var l = _layers[i];
                if (l.source == null) continue;
                float target = l.intensity == _target ? l.targetVolume : 0f;
                l.source.volume = Mathf.MoveTowards(l.source.volume, target, _fadeSpeed * Time.deltaTime);
            }
        }

        private Intensity EvaluateIntensity()
        {
            var coord = ServiceLocator.Get<GroupAICoordinator>();
            if (coord == null || _player == null) return Intensity.Calm;

            // Walk alive skeletons (the coordinator owns the list, but we scan via API-surface).
            int aware = 0;  // skeletons that detected the player
            int close = 0;  // skeletons physically close to the player
            foreach (var a in Object.FindObjectsByType<SkeletonArcher>(FindObjectsSortMode.None))
            {
                if (a == null || a.Health == null || a.Health.IsDead) continue;
                float d = Vector3.Distance(a.transform.position, _player.position);
                if (d < _detectionRange) aware++;
                if (d < _combatRange)    close++;
            }

            if (close >= 2) return Intensity.Critical;
            if (close >= 1) return Intensity.Combat;
            if (aware > 0)  return Intensity.Detected;
            if (coord.AliveCount > 0) return Intensity.Tension;
            return Intensity.Calm;
        }
    }
}
