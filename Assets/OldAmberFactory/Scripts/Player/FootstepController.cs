using UnityEngine;

namespace OldAmberFactory.Player
{
    /// <summary>
    /// Surface-aware footstep audio. Uses raycast to detect material via collider tag
    /// or a SurfaceType component on the ground collider.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FootstepController : MonoBehaviour
    {
        [System.Serializable]
        public class SurfaceSet
        {
            public string surfaceTag = "Default";
            public AudioClip[] walk;
            public AudioClip[] run;
            public AudioClip[] crouch;
        }

        [SerializeField] private AudioSource source;
        [SerializeField] private PlayerController player;
        [SerializeField] private SurfaceSet[] surfaces;
        [SerializeField] private float walkInterval = 0.55f;
        [SerializeField] private float runInterval = 0.34f;
        [SerializeField] private float crouchInterval = 0.75f;
        [SerializeField] private float volumeWalk = 0.6f;
        [SerializeField] private float volumeRun = 0.9f;
        [SerializeField] private float volumeCrouch = 0.25f;

        private CharacterController _cc;
        private float _timer;

        void Awake() { _cc = GetComponent<CharacterController>(); }

        void Update()
        {
            if (!_cc.isGrounded) return;
            Vector3 v = new Vector3(_cc.velocity.x, 0f, _cc.velocity.z);
            if (v.magnitude < 0.4f) return;

            float interval = player.IsCrouching ? crouchInterval : (player.IsRunning ? runInterval : walkInterval);
            _timer += Time.deltaTime;
            if (_timer < interval) return;
            _timer = 0f;

            var set = ResolveSurface();
            if (set == null || source == null) return;

            AudioClip[] pool = player.IsCrouching ? set.crouch : (player.IsRunning ? set.run : set.walk);
            if (pool == null || pool.Length == 0) return;

            var clip = pool[Random.Range(0, pool.Length)];
            float vol = player.IsCrouching ? volumeCrouch : (player.IsRunning ? volumeRun : volumeWalk);
            source.PlayOneShot(clip, vol);
        }

        private SurfaceSet ResolveSurface()
        {
            string tag = "Default";
            if (Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, out var hit, 1.5f,
                ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.TryGetComponent<SurfaceType>(out var st)) tag = st.surfaceTag;
                else if (!string.IsNullOrEmpty(hit.collider.tag) && hit.collider.tag != "Untagged") tag = hit.collider.tag;
            }
            foreach (var s in surfaces) if (s.surfaceTag == tag) return s;
            return surfaces != null && surfaces.Length > 0 ? surfaces[0] : null;
        }
    }

    public class SurfaceType : MonoBehaviour
    {
        public string surfaceTag = "Concrete";
    }
}
