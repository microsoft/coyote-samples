// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;
using Newtonsoft.Json;

namespace Microsoft.Coyote.Samples.CloudMessaging
{
    /// <summary>
    /// Basic implementation of a host that wraps the <see cref="Server"/> state machine
    /// instance executing in this process. The host uses the Azure Service Bus messaging
    /// framework to allow communication between the hosted server instance and all other
    /// remote servers that are part of the same Raft service, as well as the Raft client.
    /// </summary>
    internal class ServerHost : IServerHost, IServerManager, ICommunicationManager
    {
        /// <summary>
        /// The Coyote runtime responsible for executing the hosted state machine.
        /// </summary>
        private readonly IActorRuntime Runtime;

        /// <summary>
        /// Client providing access to the Azure Service Bus account.
        /// </summary>
        private readonly ManagementClient ManagementClient;

        /// <summary>
        /// Client providing access to the Azure Service Bus topic.
        /// </summary>
        private readonly ITopicClient TopicClient;

        /// <summary>
        /// Connection string to the Azure Service Bus account.
        /// </summary>
        private readonly string ConnectionString;

        /// <summary>
        /// The name of the Azure Service Bus topic.
        /// </summary>
        private readonly string TopicName;

        /// <summary>
        /// Actor id that provides access to the hosted <see cref="Server"/> state machine.
        /// </summary>
        private readonly ActorId HostedServer;

        /// <summary>
        /// Set that contains the id of each remote server in the Raft service.
        /// </summary>
        private readonly HashSet<string> RemoteServers;

        /// <summary>
        /// The id of the managed server.
        /// </summary>
        public string ServerId { get; }

        /// <summary>
        /// Collection of all remote server ids.
        /// </summary>
        public IEnumerable<string> RemoteServerIds => this.RemoteServers.ToList();

        /// <summary>
        /// Total number of servers in the service.
        /// </summary>
        public int NumServers { get; }

        /// <summary>
        /// Random generator for timeout values.
        /// </summary>
        private readonly Random RandomValueGenerator;

        /// <summary>
        /// The leader election due time.
        /// </summary>
        public TimeSpan LeaderElectionDueTime => TimeSpan.FromSeconds(this.RandomValueGenerator.Next(1, 10));

        /// <summary>
        /// The leader election periodic time interval.
        /// </summary>
        public TimeSpan LeaderElectionPeriod => TimeSpan.FromSeconds(this.RandomValueGenerator.Next(30, 60));

        public ServerHost(IActorRuntime runtime, string connectionString, string topicName,
            int serverId, int numServers)
        {
            this.Runtime = runtime;
            this.ManagementClient = new ManagementClient(connectionString);
            this.TopicClient = new TopicClient(connectionString, topicName);
            this.ConnectionString = connectionString;
            this.TopicName = topicName;
            this.NumServers = numServers;
            this.ServerId = $"Server-{serverId}";
            this.RandomValueGenerator = new Random();

            this.RemoteServers = new HashSet<string>();
            for (int id = 0; id < numServers; id++)
            {
                this.RemoteServers.Add($"Server-{id}");
            }

            // Create an actor id that will uniquely identify the server state machine
            // and act as a proxy for sending it received events by Azure Service Bus.
            this.HostedServer = this.Runtime.CreateActorIdFromName(typeof(Server), this.ServerId);
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            if (!await this.ManagementClient.SubscriptionExistsAsync(this.TopicName, this.ServerId))
            {
                await this.ManagementClient.CreateSubscriptionAsync(
                    new SubscriptionDescription(this.TopicName, this.ServerId));
            }

            // Creates and runs an instance of the Server state machine.
            this.Runtime.CreateActor(this.HostedServer, typeof(Server), new SetupServerEvent(this, this));
            this.Runtime.SendEvent(this.HostedServer, new NotifyJoinedServiceEvent());

            await this.ReceiveMessagesAsync(cancellationToken);
        }

        public async Task BroadcastVoteRequestsAsync(int term, int lastLogIndex, int lastLogTerm)
        {
            var request = new VoteRequestEvent(term, this.ServerId, lastLogIndex, lastLogTerm);
            Message message = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request)))
            {
                Label = "VoteRequest",
                ReplyTo = this.ServerId
            };

            await this.TopicClient.SendAsync(message);
        }

        public async Task SendVoteResponseAsync(string targetId, int term, bool voteGranted)
        {
            var response = new VoteResponseEvent(term, voteGranted);
            Message message = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response)))
            {
                Label = "VoteResponse",
                To = targetId,
                ReplyTo = this.ServerId
            };

            await this.TopicClient.SendAsync(message);
        }

        public async Task SendAppendEntriesRequestAsync(string targetId, int term, int prevLogIndex,
            int prevLogTerm, List<Log> entries, int leaderCommit, string command)
        {
            var request = new AppendEntriesRequestEvent(term, this.ServerId, prevLogIndex,
                prevLogTerm, entries, leaderCommit, command);
            Message message = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request)))
            {
                Label = "AppendEntriesRequest",
                To = targetId,
                ReplyTo = this.ServerId
            };

            await this.TopicClient.SendAsync(message);
        }

        public async Task SendAppendEntriesResponseAsync(string targetId, int term, bool success, string command)
        {
            var response = new AppendEntriesResponseEvent(term, success, this.ServerId, command);
            Message message = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response)))
            {
                Label = "AppendEntriesResponse",
                To = targetId,
                ReplyTo = this.ServerId
            };

            await this.TopicClient.SendAsync(message);
        }

        public async Task SendClientResponseAsync(string command)
        {
            var response = new ClientResponseEvent(command);
            Message message = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response)))
            {
                Label = "ClientResponse"
            };

            await this.TopicClient.SendAsync(message);
        }

        public void NotifyElectedLeader(int term)
        {
        }

        public async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
        {
            IMessageReceiver subscriptionReceiver = new MessageReceiver(this.ConnectionString,
                EntityNameHelper.FormatSubscriptionPath(this.TopicName, this.ServerId),
                ReceiveMode.ReceiveAndDelete);

            while (!cancellationToken.IsCancellationRequested)
            {
                // Receive the next message through Azure Service Bus.
                Message message = await subscriptionReceiver.ReceiveAsync(TimeSpan.FromMilliseconds(50));
                if (message != null)
                {
                    Event e = default;
                    string messageBody = Encoding.UTF8.GetString(message.Body);
                    if (message.Label == "ClientRequest")
                    {
                        e = JsonConvert.DeserializeObject<ClientRequestEvent>(messageBody);
                    }
                    else if (message.Label == "VoteRequest")
                    {
                        var request = JsonConvert.DeserializeObject<VoteRequestEvent>(messageBody);
                        if (request.CandidateId != this.ServerId)
                        {
                            e = request;
                        }
                    }
                    else if (message.Label == "VoteResponse" && message.To == this.ServerId)
                    {
                        e = JsonConvert.DeserializeObject<VoteResponseEvent>(messageBody);
                    }
                    else if (message.Label == "AppendEntriesRequest" && message.To == this.ServerId)
                    {
                        e = JsonConvert.DeserializeObject<AppendEntriesRequestEvent>(messageBody);
                    }
                    else if (message.Label == "AppendEntriesResponse" && message.To == this.ServerId)
                    {
                        e = JsonConvert.DeserializeObject<AppendEntriesResponseEvent>(messageBody);
                    }

                    if (e != default)
                    {
                        this.Runtime.SendEvent(this.HostedServer, e);
                    }
                }
            }

            await subscriptionReceiver.CloseAsync();
        }
    }
}
