using UnityEngine;

namespace OldAmberFactory.Audio
{
    /// <summary>
    /// Plays contextual footstep sounds based on the surface under the feet.
    /// Matches by texture tag on a SurfaceDescriptor component placed on materials or colliders.
    /// </summary>
    public class SurfaceFootsteps : MonoBehaviour
    {
        [System.Serializable]
        public class SurfaceSet
        {
            public string surfaceId;        // "concrete", "metal", "glass", "water", "dirt", "grass"
            public AudioClip[] clips;
            public float pitchMin = 0.95f;
            public float pitchMax = 1.05f;
        }

        [SerializeField] private AudioSource _source;
        [SerializeField] private SurfaceSet[] _sets;
        [SerializeField] private Transform _foot;
        [SerializeField] private float _rayDown = 1.6f;
        [SerializeField] private string _defaultSurface = "concrete";

        // Called from animation events so timing stays authored.
        public void AE_Step()
        {
            if (_foot == null) return;
            string surface = _defaultSurface;
            if (Physics.Raycast(_foot.position + Vector3.up * 0.1f, Vector3.down, out var hit, _rayDown))
            {
                var desc = hit.collider.GetComponent<SurfaceDescriptor>();
                if (desc != null) surface = desc.SurfaceId;
            }
            Play(surface);
        }

        private void Play(string surfaceId)
        {
            foreach (var s in _sets)
            {
                if (s.surfaceId != surfaceId || s.clips == null || s.clips.Length == 0) continue;
                var clip = s.clips[Random.Range(0, s.clips.Length)];
                _source.pitch = Random.Range(s.pitchMin, s.pitchMax);
                _source.PlayOneShot(clip);
                return;
            }
        }
    }

    public class SurfaceDescriptor : MonoBehaviour
    {
        [SerializeField] private string _surfaceId = "concrete";
        public string SurfaceId => _surfaceId;
    }
}
