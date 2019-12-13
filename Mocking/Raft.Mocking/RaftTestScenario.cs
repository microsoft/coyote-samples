// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Samples.CloudMessaging;

namespace Microsoft.Coyote.Samples.Mocking
{
    /// <summary>
    /// Tests the Raft service implementation by creating, hosting and executing
    /// in-memory the <see cref="Server"/> Coyote state machine instances, as well
    /// as a mock in-memory client.
    /// </summary>
    public class RaftTestScenario
    {
        /// <summary>
        /// During testing, Coyote injects a special version of the <see cref="IActorRuntime"/>
        /// that takes control of the test execution and systematically exercises interleavings
        /// and other sources of nondeterminism to find bugs in the specified scenario.
        /// </summary>
        public async Task RunTestAsync(IActorRuntime runtime, int numServers, int numRequests)
        {
            // Register a safety monitor for checking the specification that
            // only one leader can be elected at any given term.
            runtime.RegisterMonitor(typeof(SafetyMonitor));

            // Create the actor id for a client that will be sending requests to the Raft service.
            var client = runtime.CreateActorIdFromName(typeof(MockClient), "Client");

            var serverProxies = new List<ActorId>();
            for (int serverId = 0; serverId < numServers; serverId++)
            {
                // Create an actor id that will uniquely identify the server state machine
                // and act as a proxy for communicating with that state machine.
                serverProxies.Add(runtime.CreateActorIdFromName(typeof(Server), $"Server-{serverId}"));
            }

            // Create the mock server hosts for wrapping and handling communication between
            // all server state machines that execute in-memory during this test.
            var serverHosts = new List<IServerHost>();
            foreach (var serverProxy in serverProxies)
            {
                // Pass the actor id of each remote server to the host.
                serverHosts.Add(this.CreateServerHost(runtime, serverProxy, serverProxies.Where(
                    id => id != serverProxy), client));
            }

            // Start executing each server. It is important to do this only after all state machines
            // have been initialized, since each one will try to asynchronously communicate with the
            // others, and thus they have to be already bound to their corresponding actor ids (else
            // the events cannot be delivered, and the runtime will catch it as an error).
            foreach (var serverHost in serverHosts)
            {
                await serverHost.RunAsync(CancellationToken.None);
            }

            // Create the client actor instance, so the runtime starts executing it.
            runtime.CreateActor(client, typeof(MockClient), new MockClient.SetupEvent(serverProxies, numRequests));
        }

        /// <summary>
        /// Creates a new server host.
        /// </summary>
        protected virtual IServerHost CreateServerHost(IActorRuntime runtime, ActorId serverProxy,
            IEnumerable<ActorId> serverProxies, ActorId client) =>
            new MockServerHost(runtime, serverProxy, serverProxies, client);
    }
}
