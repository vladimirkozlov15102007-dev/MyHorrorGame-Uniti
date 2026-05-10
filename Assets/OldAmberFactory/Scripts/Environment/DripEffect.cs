using UnityEngine;

namespace OldAmberFactory.Environment
{
    /// <summary>
    /// Simple dripping water effect spawner. Pair with a particle burst and a drip sound.
    /// </summary>
    public class DripEffect : MonoBehaviour
    {
        [SerializeField] private ParticleSystem dropParticle;
        [SerializeField] private AudioClip[] dripClips;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private Vector2 intervalRange = new Vector2(1.5f, 4f);
        [SerializeField] private Vector3 dropOffset = new Vector3(0f, -2f, 0f);

        private float _timer;

        void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = Random.Range(intervalRange.x, intervalRange.y);

            if (dropParticle) dropParticle.Emit(1);
            if (dripClips != null && dripClips.Length > 0 && audioSource)
            {
                audioSource.transform.localPosition = dropOffset;
                audioSource.PlayOneShot(dripClips[Random.Range(0, dripClips.Length)], 0.6f);
            }
        }
    }
}
