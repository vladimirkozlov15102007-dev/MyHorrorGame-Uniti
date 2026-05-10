using System.Collections.Generic;
using UnityEngine;

namespace OldAmberFactory.AI
{
    /// <summary>
    /// Typed key-value store shared between BT nodes and AI systems.
    /// Common keys:
    ///   "player"         - Transform of the known player
    ///   "lastKnownPos"   - Vector3
    ///   "lastSeenTime"   - float
    ///   "isInCombat"     - bool
    ///   "isSuppressed"   - bool
    ///   "coverPoint"     - Vector3
    ///   "flankPoint"     - Vector3
    ///   "ambushPoint"    - Vector3
    ///   "squad"          - SquadCoordinator
    ///   "role"           - string (attacker/flanker/suppressor)
    /// </summary>
    public class Blackboard
    {
        private readonly Dictionary<string, object> _values = new();

        public void Set<T>(string key, T value) => _values[key] = value;

        public bool TryGet<T>(string key, out T value)
        {
            if (_values.TryGetValue(key, out var raw) && raw is T casted)
            {
                value = casted;
                return true;
            }
            value = default!;
            return false;
        }

        public T Get<T>(string key, T def = default)
        {
            return TryGet<T>(key, out var v) ? v : def;
        }

        public bool Has(string key) => _values.ContainsKey(key);
        public void Remove(string key) => _values.Remove(key);
    }
}
