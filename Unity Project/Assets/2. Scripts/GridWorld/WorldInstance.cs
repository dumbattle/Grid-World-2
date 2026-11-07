using System;
using System.Collections.Generic;

namespace GridWorld {
    /// <summary>
    /// Owns all overworld state. Holding it means full authority, so only the trusted root keeps a
    /// reference. Systems receive contexts from <see cref="GetContext{T}"/> instead.
    /// </summary>
    public class WorldInstance {
        public readonly EntityIDManager entityIds = new EntityIDManager();
        public readonly EntityComponentManager components = new EntityComponentManager();

        Dictionary<Type, IWorldFunctionContext> type2context = new Dictionary<Type, IWorldFunctionContext>();

        public T GetContext<T>() where T : IWorldFunctionContext, new() {
            if (type2context.TryGetValue(typeof(T), out var existing)) {
                return (T)existing;
            }

            var context = new T();
            context.Init(this);
            type2context[typeof(T)] = context;
            return context;
        }
    }
}
