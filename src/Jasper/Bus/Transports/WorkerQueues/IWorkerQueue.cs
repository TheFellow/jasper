﻿using System.Threading.Tasks;
using Jasper.Bus.Runtime;

namespace Jasper.Bus.Transports.WorkerQueues
{
    public interface IWorkerQueue
    {
        Task Enqueue(Envelope envelope);
        int QueuedCount { get; }
        void AddQueue(string queueName, int parallelization);
    }
}