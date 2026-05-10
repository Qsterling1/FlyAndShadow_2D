using System;
using System.Collections.Generic;

namespace FlyShadow.EventBus
{
    /// <summary>
    /// Centralized event bus for decoupled communication between systems.
    /// </summary>
    public static class EventManager
    {
        private static readonly Dictionary<Type, Delegate> _subscribers = new Dictionary<Type, Delegate>();

        public static void Subscribe<T>(Action<T> handler)
        {
            if (handler == null)
            {
                return;
            }

            var eventType = typeof(T);

            if (_subscribers.TryGetValue(eventType, out var existingHandlers))
            {
                if (existingHandlers is Action<T> typedHandlers)
                {
                    _subscribers[eventType] = Delegate.Combine(typedHandlers, handler);
                }
                else
                {
                    _subscribers[eventType] = handler;
                }
            }
            else
            {
                _subscribers.Add(eventType, handler);
            }
        }

        public static void Unsubscribe<T>(Action<T> handler)
        {
            if (handler == null)
            {
                return;
            }

            var eventType = typeof(T);

            if (!_subscribers.TryGetValue(eventType, out var existingHandlers) || existingHandlers == null)
            {
                return;
            }

            if (existingHandlers is Action<T> typedHandlers)
            {
                var newHandlers = Delegate.Remove(typedHandlers, handler);

                if (newHandlers == null)
                {
                    _subscribers.Remove(eventType);
                }
                else
                {
                    _subscribers[eventType] = newHandlers;
                }
            }
        }

        public static void Publish<T>(T eventData)
        {
            var eventType = typeof(T);

            if (_subscribers.TryGetValue(eventType, out var handlers) && handlers is Action<T> callback)
            {
                callback.Invoke(eventData);
            }
        }
    }
}
