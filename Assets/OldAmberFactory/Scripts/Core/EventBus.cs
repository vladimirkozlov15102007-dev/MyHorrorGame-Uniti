using System;
using System.Collections.Generic;
using UnityEngine;

namespace OldAmberFactory.Core
{
    /// <summary>
    /// Simple static event bus for global gameplay events (noise, combat, detection).
    /// </summary>
    public static class EventBus
    {
        public struct NoiseEvent
        {
            public Vector3 position;
            public float radius;
            public float intensity; // 0..1
            public GameObject source;
        }

        public struct CombatEvent
        {
            public GameObject attacker;
            public GameObject victim;
            public Vector3 position;
            public float damage;
        }

        private static readonly Dictionary<Type, List<Delegate>> _subscribers = new();

        public static void Subscribe<T>(Action<T> handler)
        {
            var key = typeof(T);
            if (!_subscribers.TryGetValue(key, out var list))
                _subscribers[key] = list = new List<Delegate>();
            list.Add(handler);
        }

        public static void Unsubscribe<T>(Action<T> handler)
        {
            if (_subscribers.TryGetValue(typeof(T), out var list))
                list.Remove(handler);
        }

        public static void Publish<T>(T evt)
        {
            if (!_subscribers.TryGetValue(typeof(T), out var list)) return;
            // Copy so handlers can unsubscribe during iteration.
            var snapshot = list.ToArray();
            foreach (var d in snapshot)
            {
                try { ((Action<T>)d)(evt); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        public static void Clear() => _subscribers.Clear();
    }
}
