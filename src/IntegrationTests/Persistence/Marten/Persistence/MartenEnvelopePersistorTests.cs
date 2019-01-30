﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jasper;
using Jasper.Messaging.Runtime;
using Jasper.Messaging.Transports;
using Jasper.Persistence;
using Jasper.Persistence.Marten.Persistence;
using Marten;
using Shouldly;
using Xunit;

namespace IntegrationTests.Persistence.Marten.Persistence
{
    public class MartenEnvelopePersistorTests : MartenContext, IDisposable
    {
        public MartenEnvelopePersistorTests()
        {
            var store = theHost.Get<IDocumentStore>();
            store.Advanced.Clean.CompletelyRemoveAll();
            theHost.RebuildMessageStorage();
        }

        public void Dispose()
        {
            theHost?.Dispose();
        }

        public IJasperHost theHost = JasperHost.For<ItemReceiver>();

        [Fact]
        public async Task get_counts()
        {
            var thePersistor = theHost.Get<MartenEnvelopePersistor>();

            var list = new List<Envelope>();

            // 10 incoming
            for (var i = 0; i < 10; i++)
            {
                var envelope = ObjectMother.Envelope();
                envelope.Status = TransportConstants.Incoming;

                list.Add(envelope);
            }

            await thePersistor.StoreIncoming(list.ToArray());


            // 7 scheduled
            list.Clear();
            for (var i = 0; i < 7; i++)
            {
                var envelope = ObjectMother.Envelope();
                envelope.Status = TransportConstants.Scheduled;

                list.Add(envelope);
            }

            await thePersistor.StoreIncoming(list.ToArray());


            // 3 outgoing
            list.Clear();
            for (var i = 0; i < 3; i++)
            {
                var envelope = ObjectMother.Envelope();
                envelope.Status = TransportConstants.Outgoing;

                list.Add(envelope);
            }

            await thePersistor.StoreOutgoing(list.ToArray(), 0);

            var counts = await thePersistor.GetPersistedCounts();

            counts.Incoming.ShouldBe(10);
            counts.Scheduled.ShouldBe(7);
            counts.Outgoing.ShouldBe(3);
        }
    }
}
