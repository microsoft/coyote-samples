// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Samples.CloudMessaging;
using Microsoft.Coyote.Samples.Mocking;

namespace Microsoft.Coyote.Samples.Nondeterminism
{
    /// <summary>
    /// Mock implementation of a server host that introduces controlled
    /// nondeterminism to exercise the specification that no more than
    /// one leader can be elected in the same term.
    /// </summary>
    public class MockServerHostWithFailure : MockServerHost
    {
        public MockServerHostWithFailure(IActorRuntime runtime, ActorId serverProxy,
            IEnumerable<ActorId> serverProxies, ActorId client)
            : base(runtime, serverProxy, serverProxies, client)
        {
        }

        /// <summary>
        /// We override this method to introduce controlled nondeterminism by invoking
        /// <see cref="IActorRuntime.Random"/> method. The returned random values are
        /// controlled by the runtime durig testing and systematically explored with
        /// other combinations of nondeterminism to find bugs.
        /// </summary>
        public override Task BroadcastVoteRequestsAsync(int term, int lastLogIndex, int lastLogTerm)
        {
            foreach (var server in this.RemoteServers.Values)
            {
                this.Runtime.SendEvent(server, new VoteRequestEvent(term, this.ServerId, lastLogIndex, lastLogTerm));
                if (this.Runtime.Random())
                {
                    // Nondeterministically send a duplicate vote to exercise the corner case
                    // where networking communication sends duplicate messages. This can cause
                    // a Raft server to count duplicate votes, leading to more than one leader
                    // being elected at the same term.
                    this.Runtime.SendEvent(server, new VoteRequestEvent(term, this.ServerId, lastLogIndex, lastLogTerm));
                }
            }

            return Task.CompletedTask;
        }
    }
}
