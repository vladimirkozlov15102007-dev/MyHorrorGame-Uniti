using System.Collections.Generic;
using UnityEngine;

namespace OldAmberFactory.AI
{
    /// <summary>
    /// A static marker placed by level design or at runtime on tagged cover props.
    /// AI queries CoverRegistry.QueryNearestAvailable for tactical movement.
    /// </summary>
    public class CoverPoint : MonoBehaviour
    {
        [SerializeField] private float _quality = 1f;
        [Tooltip("World direction the cover blocks (i.e. enemy fire comes FROM this direction).")]
        [SerializeField] private Vector3 _facing = Vector3.forward;
        [SerializeField] private float _reservationTimeout = 6f;

        private float _reservedUntil = -1f;
        public float Quality => _quality;
        public Vector3 Facing => transform.TransformDirection(_facing.normalized);

        public bool IsFree => Time.time >= _reservedUntil;

        public bool TryReserve()
        {
            if (!IsFree) return false;
            _reservedUntil = Time.time + _reservationTimeout;
            CoverRegistry.NotifyReserved(this);
            return true;
        }

        public void Release()
        {
            _reservedUntil = -1f;
            CoverRegistry.NotifyReleased(this);
        }

        /// <summary>Returns true if this cover blocks line-of-fire from the threat position.</summary>
        public bool ProtectsFrom(Vector3 threat)
        {
            Vector3 fromThreat = (transform.position - threat).normalized;
            return Vector3.Dot(fromThreat, Facing) > 0.2f;
        }

        private void OnEnable()  => CoverRegistry.Register(this);
        private void OnDisable() => CoverRegistry.Unregister(this);
    }

    public static class CoverRegistry
    {
        private static readonly List<CoverPoint> _all = new();
        public static IReadOnlyList<CoverPoint> All => _all;

        public static void Register(CoverPoint p)   { if (!_all.Contains(p)) _all.Add(p); }
        public static void Unregister(CoverPoint p) { _all.Remove(p); }
        public static void NotifyReserved(CoverPoint p) {}
        public static void NotifyReleased(CoverPoint p) {}

        public static CoverPoint QueryNearestAvailable(Vector3 from, Vector3 threat, float maxRadius)
        {
            CoverPoint best = null;
            float bestScore = float.NegativeInfinity;
            for (int i = 0; i < _all.Count; i++)
            {
                var c = _all[i];
                if (!c.IsFree) continue;
                float dist = Vector3.Distance(from, c.transform.position);
                if (dist > maxRadius) continue;
                if (!c.ProtectsFrom(threat)) continue;

                float score = c.Quality * 5f - dist;
                if (score > bestScore) { bestScore = score; best = c; }
            }
            return best;
        }
    }
}
