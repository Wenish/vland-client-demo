using System;
using System.Collections.Generic;

namespace ShadowInfection.Interactions
{
    public sealed class InteractionHandlerRegistry : IInteractionHandlerRegistry
    {
        private readonly Dictionary<InteractionType, IInteractionHandler> handlers;

        public InteractionHandlerRegistry(IEnumerable<IInteractionHandler> handlers)
        {
            this.handlers = new Dictionary<InteractionType, IInteractionHandler>();
            if (handlers == null)
                return;

            foreach (var handler in handlers)
            {
                if (handler == null)
                    continue;

                if (this.handlers.ContainsKey(handler.Type))
                    throw new InvalidOperationException($"Duplicate interaction handler for {handler.Type}.");

                this.handlers[handler.Type] = handler;
            }
        }

        public bool TryGet(InteractionType type, out IInteractionHandler handler)
        {
            return handlers.TryGetValue(type, out handler);
        }
    }
}
