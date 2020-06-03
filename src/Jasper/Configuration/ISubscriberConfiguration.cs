using System;

namespace Jasper.Configuration
{
    public interface ISubscriberConfiguration<T> where T : ISubscriberConfiguration<T>
    {
        /// <summary>
        ///     Force any messages enqueued to this worker queue to be durable
        /// </summary>
        /// <returns></returns>
        T Durably();

        /// <summary>
        /// By default, messages on this worker queue will not be persisted until
        /// being successfully handled
        /// </summary>
        /// <returns></returns>
        T QueuedInMemory();

        /// <summary>
        /// Apply envelope customization rules to any outgoing
        /// messages to this endpoint
        /// </summary>
        /// <param name="customize"></param>
        /// <returns></returns>
        T CustomizeOutgoing(Action<Envelope> customize);

        /// <summary>
        /// Apply envelope customization rules to any outgoing
        /// messages to this endpoint only for messages of either type
        /// TMessage or types that implement or inherit from TMessage
        /// </summary>
        /// <param name="customize"></param>
        /// <returns></returns>
        T CustomizeOutgoingMessagesOfType<TMessage>(Action<Envelope> customize);

        /// <summary>
        /// Apply envelope customization rules to any outgoing
        /// messages to this endpoint only for messages of either type
        /// TMessage or types that implement or inherit from TMessage
        /// </summary>
        /// <param name="customize"></param>
        /// <returns></returns>
        T CustomizeOutgoingMessagesOfType<TMessage>(Action<Envelope, TMessage> customize);


        /// <summary>
        /// Fine-tune the circuit breaker parameters for this outgoing subscriber endpoint
        /// </summary>
        /// <param name="configure"></param>
        /// <returns></returns>
        T CircuitBreaking(Action<ICircuitParameters> configure);
    }

    public static class SubscriberConfigurationExtensions
    {

    }

    public interface ISubscriberConfiguration : ISubscriberConfiguration<ISubscriberConfiguration>
    {

    }
}
