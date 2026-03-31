using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gridlock.Core
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init() => _services.Clear();

        public static void Register<T>(T service) where T : class
        {
            if (_services.ContainsKey(typeof(T)))
                Debug.LogWarning($"[ServiceLocator] Overwriting {typeof(T).Name}");
            _services[typeof(T)] = service;
        }

        public static T Get<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var service))
                return (T)service;
            return null;
        }

        public static void Unregister<T>() where T : class
        {
            _services.Remove(typeof(T));
        }

        public static void Clear()
        {
            _services.Clear();
        }
    }
}
