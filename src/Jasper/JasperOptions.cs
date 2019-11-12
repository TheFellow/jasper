﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Baseline;
using Baseline.Dates;
using Jasper.Configuration;
using Jasper.Messaging.Durability;
using Jasper.Messaging.Transports;
using Jasper.Util;
using Newtonsoft.Json;

namespace Jasper
{
    /// <summary>
    /// Configures the Jasper messaging transports in your application
    /// </summary>
    public partial class JasperOptions : ITransportsExpression
    {
        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();


        private string _serviceName = "Jasper";

        public JasperOptions()
        {
            ListenForMessagesFrom(TransportConstants.RetryUri);
            ListenForMessagesFrom(TransportConstants.ScheduledUri);
            ListenForMessagesFrom(TransportConstants.RepliesUri);

            ServiceName = "Jasper";

            UniqueNodeId = Guid.NewGuid().ToString().GetDeterministicHashCode();
        }

        [JsonIgnore] public int UniqueNodeId { get; }


        /// <summary>
        ///     Logical service name of this application used for instrumentation purposes
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        [JsonIgnore]
        public string ServiceName
        {
            get => _serviceName;
            set
            {
                if (ServiceName.IsEmpty()) throw new ArgumentNullException(nameof(ServiceName));

                _serviceName = value;
            }
        }


        /// <summary>
        /// Advanced configuration options for Jasper message processing,
        /// job scheduling, validation, and resiliency features
        /// </summary>
        public AdvancedSettings Advanced { get; } = new AdvancedSettings();


        [JsonIgnore] public CancellationToken Cancellation => _cancellation.Token;


        private readonly IList<ListenerSettings> _listeners = new List<ListenerSettings>();


        public ListenerSettings[] Listeners
        {
            get => _listeners.ToArray();
            set
            {
                _listeners.Clear();
                if (value != null) _listeners.AddRange(value);
            }
        }

        /// <summary>
        ///     Listen for messages at the given uri
        /// </summary>
        /// <param name="uri"></param>
        public IListenerSettings ListenForMessagesFrom(Uri uri)
        {
            var listener = _listeners.FirstOrDefault(x => x.Uri == uri);
            if (listener == null)
            {
                listener = new ListenerSettings
                {
                    Uri = uri
                };

                _listeners.Add(listener);
            }

            return listener;
        }

        /// <summary>
        ///     Establish a message listener to a known location and transport
        /// </summary>
        /// <param name="uriString"></param>
        public IListenerSettings ListenForMessagesFrom(string uriString)
        {
            return ListenForMessagesFrom(uriString.ToUri());
        }

    }
}
