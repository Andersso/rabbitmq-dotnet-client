﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace RabbitMQ.Client.ConsumerDispatching
{
    internal abstract class ConsumerDispatcherBase
    {
        private static readonly FallbackConsumer s_fallbackConsumer = new FallbackConsumer();
        private readonly ConcurrentDictionary<string, IAsyncBasicConsumer> _consumers = new ConcurrentDictionary<string, IAsyncBasicConsumer>();

        public IAsyncBasicConsumer? DefaultConsumer { get; set; }

        protected ConsumerDispatcherBase()
        {
        }

        protected void AddConsumer(IAsyncBasicConsumer consumer, string tag)
        {
            _consumers[tag] = consumer;
        }

        protected IAsyncBasicConsumer GetConsumerOrDefault(string tag)
        {
            return _consumers.TryGetValue(tag, out IAsyncBasicConsumer? consumer) ? consumer : GetDefaultOrFallbackConsumer();
        }

        public IAsyncBasicConsumer GetAndRemoveConsumer(string tag)
        {
            return _consumers.Remove(tag, out IAsyncBasicConsumer? consumer) ? consumer : GetDefaultOrFallbackConsumer();
        }

        public Task ShutdownAsync(ShutdownEventArgs reason)
        {
            DoShutdownConsumers(reason);
            return InternalShutdownAsync();
        }

        private void DoShutdownConsumers(ShutdownEventArgs reason)
        {
            foreach (KeyValuePair<string, IAsyncBasicConsumer> pair in _consumers.ToArray())
            {
                ShutdownConsumer(pair.Value, reason);
            }
            _consumers.Clear();
        }

        protected abstract void ShutdownConsumer(IAsyncBasicConsumer consumer, ShutdownEventArgs reason);

        protected abstract Task InternalShutdownAsync();

        // Do not inline as it's not the default case on a hot path
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IAsyncBasicConsumer GetDefaultOrFallbackConsumer()
        {
            return DefaultConsumer ?? s_fallbackConsumer;
        }
    }
}
