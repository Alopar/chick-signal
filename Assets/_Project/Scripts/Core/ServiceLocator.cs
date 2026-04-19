using System;
using System.Collections.Generic;

namespace LudumDare.Template.Core
{
    /// <summary>
    /// Minimal service locator for non-MonoBehaviour dependencies. MonoBehaviour managers still use
    /// <see cref="Singleton{T}"/>; reach for the locator only when you need decoupled resolution.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new();

        public static void Register<T>(T service) where T : class
        {
            _services[typeof(T)] = service;
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            if (_services.TryGetValue(typeof(T), out var raw))
            {
                service = (T)raw;
                return true;
            }

            service = null;
            return false;
        }

        public static T Get<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var raw))
            {
                return (T)raw;
            }

            throw new InvalidOperationException($"Service of type {typeof(T).Name} not registered.");
        }

        public static void Unregister<T>() where T : class
        {
            _services.Remove(typeof(T));
        }

        public static void Clear() => _services.Clear();
    }
}
