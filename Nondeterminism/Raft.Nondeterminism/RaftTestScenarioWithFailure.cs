// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Samples.CloudMessaging;
using Microsoft.Coyote.Samples.Mocking;

namespace Microsoft.Coyote.Samples.Nondeterminism
{
    /// <summary>
    /// Tests the Raft service implementation by creating, hosting and executing
    /// in-memory the <see cref="Server"/> Coyote state machine instances, as well
    /// as a mock in-memory client.
    /// </summary>
    public class RaftTestScenarioWithFailure : RaftTestScenario
    {
        /// <summary>
        /// Creates a new server host.
        /// </summary>
        protected override IServerHost CreateServerHost(IActorRuntime runtime, ActorId serverProxy,
            IEnumerable<ActorId> serverProxies, ActorId client) =>
            new MockServerHostWithFailure(runtime, serverProxy, serverProxies, client);
    }
}
