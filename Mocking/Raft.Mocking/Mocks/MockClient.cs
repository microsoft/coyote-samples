// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Actors.Timers;
using Microsoft.Coyote.Samples.CloudMessaging;

namespace Microsoft.Coyote.Samples.Mocking
{
    /// <summary>
    /// Mock implementation of a client that sends requests to
    /// Raft server instances.
    /// </summary>
    [OnEventDoAction(typeof(ClientResponseEvent), nameof(HandleResponse))]
    [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeout))]
    internal class MockClient : Actor
    {
        internal class SetupEvent : Event
        {
            internal readonly IEnumerable<ActorId> Servers;
            internal readonly int NumRequests;

            internal SetupEvent(IEnumerable<ActorId> servers, int numRequests)
            {
                this.Servers = servers;
                this.NumRequests = numRequests;
            }
        }

        private IEnumerable<ActorId> Servers;
        private int NumRequests;
        private int NumResponses;

        private string NextCommand => $"request-{this.NumResponses}";

        protected override Task OnInitializeAsync(Event initialEvent)
        {
            var setup = initialEvent as SetupEvent;
            this.Servers = setup.Servers;
            this.NumRequests = setup.NumRequests;
            this.NumResponses = 0;

            // Start by sending the first request.
            this.SendNextRequest();

            // Create a periodic timer to retry sending requests, if needed.
            // The chosen time does not matter, as the client will run under
            // test mode, and thus the time is controlled by the runtime.
            this.StartPeriodicTimer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            return Task.CompletedTask;
        }

        private void SendNextRequest()
        {
            foreach (var server in this.Servers)
            {
                // We naively sent the request to all servers, but this could be optimized
                // by providing an intermediate "service" mock actor that redirects events.
                this.SendEvent(server, new ClientRequestEvent(this.NextCommand));
            }

            this.Logger.WriteLine($"<Client> sent {this.NextCommand}.");
        }

        private void HandleResponse(Event e)
        {
            var response = e as ClientResponseEvent;
            if (response.Command == this.NextCommand)
            {
                this.Logger.WriteLine($"<Client> received response for {response.Command}.");
                this.NumResponses++;

                if (this.NumResponses == this.NumRequests)
                {
                    // Halt the client, as all responses have been received.
                    this.Halt();
                }
                else
                {
                    this.SendNextRequest();
                }
            }
        }

        /// <summary>
        /// Retry to send the request.
        /// </summary>
        private void HandleTimeout() => this.SendNextRequest();
    }
}
