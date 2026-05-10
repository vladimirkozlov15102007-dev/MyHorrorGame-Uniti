using UnityEngine;

namespace OldAmberFactory.Audio
{
    /// <summary>
    /// Layered adaptive music. Assign AudioSources for each layer. Target volumes
    /// crossfade smoothly based on current GameState.
    ///   calm      - quiet drone, always on
    ///   tension   - rises when enemies hear the player
    ///   detect    - rises when enemies see the player
    ///   combat    - full intensity during active combat
    ///   critical  - low HP stinger layer
    /// </summary>
    public class AdaptiveMusic : MonoBehaviour
    {
        public enum Mood { Calm, Tension, Detect, Combat, Critical }

        [SerializeField] private AudioSource calm;
        [SerializeField] private AudioSource tension;
        [SerializeField] private AudioSource detect;
        [SerializeField] private AudioSource combat;
        [SerializeField] private AudioSource critical;

        [SerializeField] private float fadeSpeed = 0.8f;

        private float _tCalm = 1f, _tTension, _tDetect, _tCombat, _tCritical;

        public void SetMood(Mood m)
        {
            _tCalm = 1f;
            _tTension = 0f; _tDetect = 0f; _tCombat = 0f; _tCritical = 0f;
            switch (m)
            {
                case Mood.Calm: break;
                case Mood.Tension: _tTension = 0.85f; break;
                case Mood.Detect: _tTension = 0.6f; _tDetect = 0.85f; break;
                case Mood.Combat: _tDetect = 0.5f; _tCombat = 1f; break;
                case Mood.Critical: _tCombat = 1f; _tCritical = 1f; break;
            }
        }

        void Update()
        {
            Fade(calm, _tCalm);
            Fade(tension, _tTension);
            Fade(detect, _tDetect);
            Fade(combat, _tCombat);
            Fade(critical, _tCritical);
        }

        void Fade(AudioSource s, float target)
        {
            if (!s) return;
            if (!s.isPlaying && target > 0.01f) { s.loop = true; s.Play(); }
            s.volume = Mathf.MoveTowards(s.volume, target, fadeSpeed * Time.deltaTime);
        }
    }
}
