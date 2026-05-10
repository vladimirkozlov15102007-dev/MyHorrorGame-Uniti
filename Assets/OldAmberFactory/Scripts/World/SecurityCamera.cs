using UnityEngine;

namespace OldAmberFactory.World
{
    /// <summary>
    /// A Unity camera rendering into a RenderTexture. The SecurityRoom picks the
    /// active one and shows its output on the CRT monitor.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class SecurityCamera : MonoBehaviour
    {
        [SerializeField] private RenderTexture _output;
        [SerializeField] private bool _alwaysOn = false;

        private Camera _cam;
        public RenderTexture Output => _output;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            _cam.targetTexture = _output;
            _cam.enabled = _alwaysOn;
        }

        public void SetRendering(bool on) => _cam.enabled = on || _alwaysOn;
    }
}
