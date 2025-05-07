using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectCoffee.Core.Services
{
    /// <summary>
    /// Service locator pattern implementation to manage service dependencies
    /// </summary>
    public class ServiceLocator
    {
        private static ServiceLocator _instance;
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        
        // Singleton access
        public static ServiceLocator Instance => _instance ??= new ServiceLocator();
        
        // Constructor is private to enforce singleton pattern
        private ServiceLocator() { }
        
        /// <summary>
        /// Register a service with the service locator
        /// </summary>
        /// <typeparam name="T">Service interface type</typeparam>
        /// <param name="service">Service implementation</param>
        public void RegisterService<T>(T service) where T : class
        {
            Type type = typeof(T);
            
            if (_services.ContainsKey(type))
            {
                Debug.LogWarning($"Replacing existing service of type {type.Name}");
                _services[type] = service;
            }
            else
            {
                _services.Add(type, service);
                Debug.Log($"Registered service of type {type.Name}");
            }
        }
        
        /// <summary>
        /// Get a service from the service locator
        /// </summary>
        /// <typeparam name="T">Service interface type</typeparam>
        /// <returns>Service implementation or null if not found</returns>
        public T GetService<T>() where T : class
        {
            Type type = typeof(T);
            
            if (_services.TryGetValue(type, out var service))
            {
                return (T)service;
            }
            
            // Only log a warning if we're in the editor (for debugging)
            #if UNITY_EDITOR
            Debug.LogWarning($"Service of type {type.Name} not found!");
            #endif
            
            return null;
        }
        
        /// <summary>
        /// Check if a service is registered
        /// </summary>
        /// <typeparam name="T">Service interface type</typeparam>
        /// <returns>True if the service is registered</returns>
        public bool HasService<T>() where T : class
        {
            return _services.ContainsKey(typeof(T));
        }
        
        /// <summary>
        /// Remove a service from the service locator
        /// </summary>
        /// <typeparam name="T">Service interface type</typeparam>
        public void RemoveService<T>() where T : class
        {
            Type type = typeof(T);
            
            if (_services.ContainsKey(type))
            {
                _services.Remove(type);
                Debug.Log($"Removed service of type {type.Name}");
            }
        }
        
        /// <summary>
        /// Clear all registered services
        /// </summary>
        public void Clear()
        {
            _services.Clear();
            Debug.Log("All services cleared");
        }
    }
}