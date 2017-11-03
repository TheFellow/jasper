﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jasper.Bus.Runtime;
using Jasper.Bus.Runtime.Invocation;
using Jasper.Bus.Transports.Sending;

namespace Jasper.Bus.Transports.Stub
{
    public class StubChannel : ISendingAgent, IDisposable
    {
        private readonly StubTransport _stubTransport;
        private readonly IHandlerPipeline _pipeline;
        public readonly IList<StubMessageCallback> Callbacks = new List<StubMessageCallback>();

        public StubChannel(Uri destination, IHandlerPipeline pipeline, StubTransport stubTransport)
        {
            _pipeline = pipeline;
            _stubTransport = stubTransport;
            Destination = destination;
        }

        public void Dispose()
        {

        }

        public Uri Destination { get; }
        public Uri DefaultReplyUri { get; set; }

        public Task EnqueueOutgoing(Envelope envelope)
        {
            envelope.ReceivedAt = Destination;
            envelope.ReplyUri = envelope.ReplyUri ?? DefaultReplyUri;

            var callback = new StubMessageCallback(this);
            Callbacks.Add(callback);

            _stubTransport.Callbacks.Add(callback);

            envelope.Callback = callback;

            envelope.ReceivedAt = Destination;

            return _pipeline.Invoke(envelope);
        }

        public Task StoreAndForward(Envelope envelope)
        {
            return EnqueueOutgoing(envelope);
        }

        public void Start()
        {

        }
    }
}
