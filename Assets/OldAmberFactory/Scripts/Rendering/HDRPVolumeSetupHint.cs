using UnityEngine;

namespace OldAmberFactory.Rendering
{
    /// <summary>
    /// Not a runtime component, but a scene marker attached to the global Volume
    /// in the starter scene so artists/TAs know what must be toggled in the Volume Profile:
    ///
    ///   - Fog (Volumetric, Density 0.6, Mean FreePath 32, Fog Color 0.12/0.14/0.17)
    ///   - Exposure (Automatic Histogram, Compensation -0.6)
    ///   - Screen Space Reflections (Quality High, Preset HIGH for PS5-class, Max distance 64)
    ///   - Screen Space Global Illumination (Full Resolution, Max Ray Steps 64)
    ///   - Screen Space Ambient Occlusion (Intensity 1.2, Radius 0.45)
    ///   - Contact Shadows (Length 0.15, Quality High)
    ///   - Shadows (Max Distance 80m, Cascade 4)
    ///   - Bloom (Intensity 0.18, Scatter 0.7)
    ///   - Vignette (Intensity 0.22)
    ///   - Film Grain (Intensity 0.18, Response 0.8)
    ///   - Depth of Field (Physical Camera, Focus Distance 4m while ADS)
    ///   - Motion Blur (Intensity 0.35)
    ///   - Chromatic Aberration (Intensity 0.08)
    ///   - Color Grading LUT: cold-teal-amber horror preset (see Assets/OldAmberFactory/LUTs)
    ///   - Ray Tracing (if hardware supports): Enable RTR, RTGI, RTAO, Path Traced Shadows
    ///
    /// Lighting:
    ///   - Directional Light (moon): temperature 5800K, intensity 0.35 (lux), soft shadows,
    ///     angular diameter 0.7 (for larger moon disc).
    ///   - Local lights: spot emergency lamps on flicker timeline; point lights in muzzle/shell VFX.
    ///   - Reflection Probes: baked in key rooms + planar on wet floors.
    ///
    /// Outdoor (final zone):
    ///   - Sun: temperature 5800K, intensity 90,000 lux, angular diameter 0.5.
    ///   - Fog: density 0.35 (thinner), ground fog enabled.
    ///   - Physically Based Sky, with Planetary Radius 6360 km.
    ///   - Cloud Layer enabled with high altitude wisps.
    /// </summary>
    public class HDRPVolumeSetupHint : MonoBehaviour
    {
    }
}
