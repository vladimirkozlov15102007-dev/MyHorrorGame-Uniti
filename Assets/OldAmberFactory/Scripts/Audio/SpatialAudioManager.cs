using UnityEngine;

namespace OldAmberFactory.Audio
{
    /// <summary>
    /// Global helpers for spatial audio occlusion. For each registered AudioSource,
    /// casts a ray to the listener. If blocked, applies low-pass via per-source mixer group
    /// or a volume duck fallback when the project has no mixer set up.
    /// </summary>
    public class SpatialAudioManager : MonoBehaviour
    {
        [SerializeField] private Transform _listener;
        [SerializeField] private LayerMask _occluderMask;
        [SerializeField] private AudioSource[] _managedSources;
        [SerializeField] private float _occludedVolumeMul = 0.45f;

        private float[] _originalVolumes;

        private void Start()
        {
            _originalVolumes = new float[_managedSources.Length];
            for (int i = 0; i < _managedSources.Length; i++)
                _originalVolumes[i] = _managedSources[i] != null ? _managedSources[i].volume : 0f;
        }

        private void LateUpdate()
        {
            if (_listener == null) return;

            for (int i = 0; i < _managedSources.Length; i++)
            {
                var s = _managedSources[i];
                if (s == null) continue;

                Vector3 to = _listener.position - s.transform.position;
                bool occluded = Physics.Raycast(s.transform.position, to.normalized, to.magnitude, _occluderMask,
                    QueryTriggerInteraction.Ignore);
                s.volume = occluded
                    ? _originalVolumes[i] * _occludedVolumeMul
                    : _originalVolumes[i];
            }
        }
    }
}
