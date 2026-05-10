using UnityEngine;

namespace OldAmberFactory.Environment
{
    /// <summary>
    /// On player enter, swap HDRP post-fx area (indoor/outdoor/escape).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class AreaTrigger : MonoBehaviour
    {
        [SerializeField] private Graphics.HDRPPostFXController postFx;
        [SerializeField] private Graphics.HDRPPostFXController.Area area = Graphics.HDRPPostFXController.Area.IndoorIndustrial;

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            if (postFx) postFx.SetArea(area);
        }
    }
}
