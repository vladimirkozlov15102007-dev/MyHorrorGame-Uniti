using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace OldAmberFactory.Graphics
{
    /// <summary>
    /// Drives HDRP Volume parameters cinematically based on game state:
    ///  - Indoor: heavy fog, cold tint, low exposure
    ///  - Outdoor day: bright exposure, sunlight directional, light fog
    ///  - Combat: slight chromatic aberration / film grain bump
    ///
    /// This component expects a Volume component on the same GameObject with overrides
    /// for Fog, Exposure, ColorAdjustments, Vignette, DepthOfField. Missing overrides are ignored.
    /// </summary>
    [RequireComponent(typeof(Volume))]
    public class HDRPPostFXController : MonoBehaviour
    {
        public enum Area { IndoorIndustrial, OutdoorDay, Escape }

        [SerializeField] private Area currentArea = Area.IndoorIndustrial;
        [SerializeField] private float blendSpeed = 1.2f;

        Volume _volume;
        Fog _fog;
        Exposure _exposure;
        ColorAdjustments _color;
        Vignette _vignette;
        DepthOfField _dof;
        WhiteBalance _whiteBalance;

        void Awake()
        {
            _volume = GetComponent<Volume>();
            if (_volume.profile == null) _volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _volume.profile.TryGet(out _fog);
            _volume.profile.TryGet(out _exposure);
            _volume.profile.TryGet(out _color);
            _volume.profile.TryGet(out _vignette);
            _volume.profile.TryGet(out _dof);
            _volume.profile.TryGet(out _whiteBalance);
        }

        public void SetArea(Area a) { currentArea = a; }

        void Update()
        {
            switch (currentArea)
            {
                case Area.IndoorIndustrial:
                    ApplyIndoor();
                    break;
                case Area.OutdoorDay:
                    ApplyOutdoorDay();
                    break;
                case Area.Escape:
                    ApplyEscape();
                    break;
            }
        }

        void ApplyIndoor()
        {
            if (_fog != null)
            {
                _fog.meanFreePath.value = Mathf.Lerp(_fog.meanFreePath.value, 35f, Time.deltaTime * blendSpeed);
                _fog.baseHeight.value = 1.5f;
            }
            if (_exposure != null) _exposure.fixedExposure.value = Mathf.Lerp(_exposure.fixedExposure.value, 9f, Time.deltaTime * blendSpeed);
            if (_color != null)
            {
                _color.colorFilter.value = Color.Lerp(_color.colorFilter.value, new Color(0.75f, 0.85f, 1f), Time.deltaTime * blendSpeed);
                _color.saturation.value = Mathf.Lerp(_color.saturation.value, -12f, Time.deltaTime * blendSpeed);
            }
            if (_whiteBalance != null) _whiteBalance.temperature.value = Mathf.Lerp(_whiteBalance.temperature.value, -20f, Time.deltaTime * blendSpeed);
            if (_vignette != null) _vignette.intensity.value = Mathf.Lerp(_vignette.intensity.value, 0.35f, Time.deltaTime * blendSpeed);
        }

        void ApplyOutdoorDay()
        {
            if (_fog != null)
            {
                _fog.meanFreePath.value = Mathf.Lerp(_fog.meanFreePath.value, 2000f, Time.deltaTime * blendSpeed);
                _fog.baseHeight.value = 8f;
            }
            if (_exposure != null) _exposure.fixedExposure.value = Mathf.Lerp(_exposure.fixedExposure.value, 14f, Time.deltaTime * blendSpeed);
            if (_color != null)
            {
                _color.colorFilter.value = Color.Lerp(_color.colorFilter.value, new Color(1.02f, 1.0f, 0.96f), Time.deltaTime * blendSpeed);
                _color.saturation.value = Mathf.Lerp(_color.saturation.value, 4f, Time.deltaTime * blendSpeed);
            }
            if (_whiteBalance != null) _whiteBalance.temperature.value = Mathf.Lerp(_whiteBalance.temperature.value, 5f, Time.deltaTime * blendSpeed);
            if (_vignette != null) _vignette.intensity.value = Mathf.Lerp(_vignette.intensity.value, 0.15f, Time.deltaTime * blendSpeed);
        }

        void ApplyEscape()
        {
            // Cinematic heavy DoF + moody fog.
            if (_dof != null)
            {
                _dof.focusDistance.value = Mathf.Lerp(_dof.focusDistance.value, 12f, Time.deltaTime * blendSpeed);
            }
            ApplyOutdoorDay();
            if (_color != null) _color.saturation.value = Mathf.Lerp(_color.saturation.value, -6f, Time.deltaTime * blendSpeed);
        }
    }
}
