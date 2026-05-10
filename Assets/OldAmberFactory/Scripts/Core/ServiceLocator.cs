using System;
using System.Collections.Generic;
using UnityEngine;

namespace OldAmberFactory.Core
{
    /// <summary>
    /// Minimal service locator so systems (audio, music, AI coordinator, game manager)
    /// can find each other without hard singleton patterns scattered across the codebase.
    /// Services register themselves in Awake() and are queried by consumers.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new();

        public static void Register<T>(T instance) where T : class
        {
            _services[typeof(T)] = instance;
        }

        public static void Unregister<T>() where T : class
        {
            _services.Remove(typeof(T));
        }

        public static T Get<T>() where T : class
        {
            return _services.TryGetValue(typeof(T), out var s) ? s as T : null;
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            if (_services.TryGetValue(typeof(T), out var s))
            {
                service = s as T;
                return service != null;
            }
            service = null;
            return false;
        }

        public static void ClearAll() => _services.Clear();
    }
}
