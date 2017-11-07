﻿using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Jasper.Bus.Runtime;
using Jasper.Bus.Transports.Tcp;
using Jasper.Bus.Transports.Util;

namespace Jasper.Bus.Transports.Sending
{
    public class BatchedSender : ISender
    {
        public Uri Destination { get; }

        private readonly ISenderProtocol _protocol;
        private readonly CancellationToken _cancellation;
        private ISenderCallback _callback;
        private ActionBlock<OutgoingMessageBatch> _sender;
        private BatchingBlock<Envelope> _batching;
        private int _queued = 0;
        private ActionBlock<Envelope> _serializing;
        private TransformBlock<Envelope[], OutgoingMessageBatch> _batchWriting;

        public BatchedSender(Uri destination, ISenderProtocol protocol, CancellationToken cancellation)
        {
            Destination = destination;
            _protocol = protocol;
            _cancellation = cancellation;
        }

        public void Start(ISenderCallback callback)
        {
            _callback = callback;

            _sender = new ActionBlock<OutgoingMessageBatch>(sendBatch, new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = 1,
                CancellationToken = _cancellation
            });

            _serializing = new ActionBlock<Envelope>(e =>
            {
                try
                {
                    e.EnsureData();
                    _batching.Post(e);
                }
                catch (Exception ex)
                {
                    // TODO -- GOTSTA LOG THIS AND MAYBE RECOVER THE ENVELOPE
                }
            },
            new ExecutionDataflowBlockOptions
            {
                CancellationToken = _cancellation
            });


            _batchWriting = new TransformBlock<Envelope[], OutgoingMessageBatch>(
                envelopes => new OutgoingMessageBatch(Destination, envelopes));

            _batchWriting.LinkTo(_sender);

            _batching = new BatchingBlock<Envelope>(200, _batchWriting, _cancellation);


        }

        public int QueuedCount => _queued + _batching.ItemCount;

        private async Task sendBatch(OutgoingMessageBatch batch)
        {
            _queued += batch.Messages.Count;


            try
            {
                await _protocol.SendBatch(_callback, batch);
            }
            catch (Exception e)
            {
                batchSendFailed(batch, e);
            }
            finally
            {
                _queued -= batch.Messages.Count;
            }
        }

        private void batchSendFailed(OutgoingMessageBatch batch, Exception exception)
        {
            _callback.ProcessingFailure(batch, exception);
        }

        public Task Enqueue(Envelope message)
        {
            if (_batching == null) throw new InvalidOperationException("This agent has not been started");

            _serializing.Post(message);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _serializing?.Complete();
            _sender?.Complete();
            _batching?.Dispose();
        }
    }
}
