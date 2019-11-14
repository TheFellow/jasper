﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Jasper.Configuration;
using Jasper.Messaging.Durability;
using Jasper.Messaging.Logging;
using Jasper.Messaging.Model;
using Jasper.Messaging.Runtime;
using Jasper.Messaging.Runtime.Invocation;
using Jasper.Messaging.Runtime.Routing;
using Jasper.Messaging.Runtime.Serializers;
using Jasper.Messaging.Scheduled;
using Jasper.Messaging.Transports;
using Jasper.Messaging.Transports.Sending;
using Jasper.Messaging.WorkerQueues;
using Lamar;
using LamarCodeGeneration;

namespace Jasper.Messaging
{
    public interface IMessagingRoot : ISubscriberGraph
    {
        IScheduledJobProcessor ScheduledJobs { get; }
        IMessageRouter Router { get; }
        IHandlerPipeline Pipeline { get; }
        IMessageLogger Logger { get; }
        MessagingSerializationGraph Serialization { get; }
        JasperOptions Options { get; }


        ITransport[] Transports { get; }
        HandlerGraph Handlers { get; }

        IMessageContext NewContext();
        IMessageContext ContextFor(Envelope envelope);

        IEnvelopePersistence Persistence { get; }
        ITransportLogger TransportLogger { get; }
        AdvancedSettings Settings { get; }

        [Obsolete("Get rid of this")]
        ISendingAgent BuildDurableSendingAgent(Uri destination, ISender sender);

        [Obsolete("Get rid of this")]
        ISendingAgent BuildDurableLoopbackAgent(Uri destination);

        void AddListener(ListenerSettings listenerSettings, IListeningAgent agent);
    }
}
