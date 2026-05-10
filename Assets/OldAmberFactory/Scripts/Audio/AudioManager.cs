using UnityEngine;

namespace OldAmberFactory.Audio
{
    /// <summary>
    /// Global audio entry point. Plays one-shots in 3D space and routes ambient/music layers.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private AudioSource oneShotPrototype;
        [SerializeField] private AdaptiveMusic music;

        public AdaptiveMusic Music => music;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void PlayOneShot3D(AudioClip clip, Vector3 pos, float volume = 1f, float spatialBlend = 1f)
        {
            if (!clip) return;
            var go = new GameObject("OneShot3D");
            go.transform.position = pos;
            var src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.volume = volume;
            src.spatialBlend = spatialBlend;
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.maxDistance = 40f;
            src.Play();
            Destroy(go, clip.length + 0.1f);
        }
    }
}
