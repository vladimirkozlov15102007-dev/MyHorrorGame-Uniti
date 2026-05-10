using UnityEngine;
using UnityEngine.Rendering;

namespace OldAmberFactory.Rendering
{
    /// <summary>
    /// Runtime sanity-check. Logs warnings if the project is not actually running HDRP
    /// or if the color space is not Linear.
    ///
    /// Scene-setup checklist (Editor-only, see README):
    ///   1. Edit > Project Settings > Graphics: assign the HDRenderPipelineAsset.
    ///   2. Edit > Project Settings > Quality: assign Quality-tier HDRP RP Assets.
    ///   3. Edit > Project Settings > Player > Other Settings: Color Space = Linear.
    ///   4. Window > Rendering > Render Pipeline Wizard > Fix All (HDRP).
    /// </summary>
    public class HDRPBootstrap : MonoBehaviour
    {
        private void Start()
        {
            if (GraphicsSettings.defaultRenderPipeline == null)
                Debug.LogWarning("[HDRPBootstrap] No Render Pipeline Asset assigned. This project expects HDRP.");

            if (QualitySettings.activeColorSpace != ColorSpace.Linear)
                Debug.LogWarning("[HDRPBootstrap] Color space must be Linear for correct PBR.");
        }
    }
}
