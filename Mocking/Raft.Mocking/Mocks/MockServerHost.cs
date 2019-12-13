// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
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
    /// Mock implementation of a host that wraps one of the <see cref="Server"/>
    /// state machine instances executing as part of a Raft service. The host
    /// maintains a set of all <see cref="Server"/> instances and allows them
    /// to communicate in-memory, so that Coyote can systematically test the
    /// Raft service logic.
    /// </summary>
    public class MockServerHost : IServerHost, IServerManager, ICommunicationManager
    {
        /// <summary>
        /// The Coyote runtime responsible for executing the hosted state machine.
        /// </summary>
        protected readonly IActorRuntime Runtime;

        /// <summary>
        /// Actor id that provides access to the hosted <see cref="Server"/> state machine.
        /// </summary>
        protected readonly ActorId ServerProxy;

        /// <summary>
        /// Set that contains the actor id of each remote server in the Raft service.
        /// </summary>
        protected readonly Dictionary<string, ActorId> RemoteServers;

        /// <summary>
        /// The Raft client.
        /// </summary>
        protected readonly ActorId Client;

        /// <summary>
        /// The id of the managed server.
        /// </summary>
        public string ServerId { get; }

        /// <summary>
        /// Collection of all remote server ids.
        /// </summary>
        public IEnumerable<string> RemoteServerIds { get; }

        /// <summary>
        /// Total number of servers in the service.
        /// </summary>
        public int NumServers { get; }

        /// <summary>
        /// The leader election due time.
        /// </summary>
        public TimeSpan LeaderElectionDueTime => TimeSpan.FromSeconds(1);

        /// <summary>
        /// The leader election periodic time interval.
        /// </summary>
        public TimeSpan LeaderElectionPeriod => TimeSpan.FromSeconds(1);

        public MockServerHost(IActorRuntime runtime, ActorId serverProxy,
            IEnumerable<ActorId> serverProxies, ActorId client)
        {
            this.Runtime = runtime;
            this.ServerProxy = serverProxy;
            this.ServerId = serverProxy.Name;

            this.RemoteServers = new Dictionary<string, ActorId>();
            foreach (var server in serverProxies)
            {
                this.RemoteServers.Add(server.Name, server);
            }

            this.RemoteServerIds = this.RemoteServers.Keys.ToList();
            this.NumServers = this.RemoteServers.Count + 1;
            this.Client = client;

            // Creates an instance of the Server state machine and associates
            // it with the given actor id.
            this.Runtime.CreateActor(this.ServerProxy, typeof(Server), new SetupServerEvent(this, this));
        }

        public Task RunAsync(CancellationToken cancellationToken)
        {
            this.Runtime.SendEvent(this.ServerProxy, new NotifyJoinedServiceEvent());
            return Task.CompletedTask;
        }

        public virtual Task BroadcastVoteRequestsAsync(int term, int lastLogIndex, int lastLogTerm)
        {
            foreach (var server in this.RemoteServers.Values)
            {
                this.Runtime.SendEvent(server, new VoteRequestEvent(term, this.ServerId, lastLogIndex, lastLogTerm));
            }

            return Task.CompletedTask;
        }

        public Task SendVoteResponseAsync(string targetId, int term, bool voteGranted)
        {
            this.Runtime.SendEvent(this.RemoteServers[targetId], new VoteResponseEvent(term, voteGranted));
            return Task.CompletedTask;
        }

        public Task SendAppendEntriesRequestAsync(string targetId, int term, int prevLogIndex,
            int prevLogTerm, List<Log> entries, int leaderCommit, string command)
        {
            this.Runtime.SendEvent(this.RemoteServers[targetId], new AppendEntriesRequestEvent(
                term, this.ServerId, prevLogIndex, prevLogTerm, entries, leaderCommit, command));
            return Task.CompletedTask;
        }

        public Task SendAppendEntriesResponseAsync(string targetId, int term, bool success, string command)
        {
            this.Runtime.SendEvent(this.RemoteServers[targetId], new AppendEntriesResponseEvent(
                term, success, this.ServerId, command));
            return Task.CompletedTask;
        }

        public Task SendClientResponseAsync(string command)
        {
            this.Runtime.SendEvent(this.Client, new ClientResponseEvent(command));
            return Task.CompletedTask;
        }

        public void NotifyElectedLeader(int term)
        {
            this.Runtime.InvokeMonitor<SafetyMonitor>(new SafetyMonitor.NotifyLeaderElected(term));
        }
    }
}
