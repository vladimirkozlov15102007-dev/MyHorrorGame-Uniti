using System.Collections.Generic;
using UnityEngine;

namespace OldAmberFactory.AI.BehaviorTree
{
    /// <summary>
    /// Typed key/value blackboard shared between the behaviour tree, utility scorers,
    /// and any external advisor (e.g. the group coordinator).
    /// </summary>
    public class Blackboard
    {
        private readonly Dictionary<string, object> _data = new();

        public void Set<T>(string key, T value) => _data[key] = value;
        public bool Has(string key) => _data.ContainsKey(key);
        public void Clear(string key) => _data.Remove(key);

        public T Get<T>(string key, T fallback = default)
        {
            if (_data.TryGetValue(key, out var v) && v is T t) return t;
            return fallback;
        }

        public bool TryGet<T>(string key, out T value)
        {
            if (_data.TryGetValue(key, out var v) && v is T t)
            {
                value = t;
                return true;
            }
            value = default;
            return false;
        }
    }

    /// <summary>
    /// Canonical blackboard keys used across the skeleton AI.
    /// Defined in one place so tasks, states and coordinator agree on the schema.
    /// </summary>
    public static class BBKeys
    {
        public const string PlayerRef        = "playerRef";
        public const string LastKnownPos     = "lastKnownPos";
        public const string SawPlayer        = "sawPlayer";
        public const string TimeSinceSeen    = "timeSinceSeen";
        public const string HeardNoisePos    = "heardNoisePos";
        public const string TimeSinceHeard   = "timeSinceHeard";
        public const string AssignedRole     = "assignedRole";   // see GroupRole enum
        public const string FlankDir         = "flankDir";       // +1 right, -1 left
        public const string CurrentCoverPos  = "currentCoverPos";
        public const string SuppressTarget   = "suppressTarget"; // world pos
        public const string HealthNorm       = "healthNorm";
        public const string AmbushPoint      = "ambushPoint";
        public const string PatrolPoint      = "patrolPoint";
    }

    public enum GroupRole
    {
        None       = 0,
        Assault    = 1,
        FlankLeft  = 2,
        FlankRight = 3,
        Suppressor = 4,
        Ambusher   = 5,
        Support    = 6
    }
}
