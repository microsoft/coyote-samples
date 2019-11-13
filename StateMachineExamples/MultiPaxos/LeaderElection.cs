// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Coyote;
using Microsoft.Coyote.Actors;

namespace Coyote.Examples.MultiPaxos
{
    internal class LeaderElection : StateMachine
    {
        internal class Config : Event
        {
            public List<ActorId> Servers;
            public ActorId ParentServer;
            public int MyRank;

            public Config(List<ActorId> servers, ActorId parentServer, int myRank)
            {
                this.Servers = servers;
                this.ParentServer = parentServer;
                this.MyRank = myRank;
            }
        }

        internal class Ping : Event
        {
            public ActorId LeaderElection;
            public int Rank;

            public Ping(ActorId leaderElection, int rank)
            {
                this.LeaderElection = leaderElection;
                this.Rank = rank;
            }
        }

        internal class NewLeader : Event
        {
            public ActorId CurrentLeader;
            public int Rank;

            public NewLeader(ActorId leader, int rank)
            {
                this.CurrentLeader = leader;
                this.Rank = rank;
            }
        }

        private List<ActorId> Servers;
        private ActorId ParentServer;
        private int MyRank;
        private Tuple<int, ActorId> CurrentLeader;
        private ActorId CommunicateLeaderTimeout;
        private ActorId BroadCastTimeout;

        [Start]
        [OnEventGotoState(typeof(Local), typeof(ProcessPings))]
        [OnEventDoAction(typeof(LeaderElection.Config), nameof(Configure))]
        private class Init : State { }

        private void Configure()
        {
            this.Servers = (this.ReceivedEvent as LeaderElection.Config).Servers;
            this.ParentServer = (this.ReceivedEvent as LeaderElection.Config).ParentServer;
            this.MyRank = (this.ReceivedEvent as LeaderElection.Config).MyRank;

            this.CurrentLeader = Tuple.Create(this.MyRank, this.Id);

            this.CommunicateLeaderTimeout = this.CreateActor(typeof(Timer));
            this.SendEvent(this.CommunicateLeaderTimeout, new Timer.Config(this.Id, 100));

            this.BroadCastTimeout = this.CreateActor(typeof(Timer));
            this.SendEvent(this.BroadCastTimeout, new Timer.Config(this.Id, 10));

            this.RaiseEvent(new Local());
        }

        [OnEntry(nameof(ProcessPingsOnEntry))]
        [OnEventGotoState(typeof(Timer.TimeoutEvent), typeof(ProcessPings), nameof(ProcessPingsAction))]
        [OnEventDoAction(typeof(LeaderElection.Ping), nameof(CalculateLeader))]
        private class ProcessPings : State { }

        private void ProcessPingsOnEntry()
        {
            foreach (var server in this.Servers)
            {
                this.SendEvent(server, new LeaderElection.Ping(this.Id, this.MyRank));
            }

            this.SendEvent(this.BroadCastTimeout, new Timer.StartTimerEvent());
        }

        private void ProcessPingsAction()
        {
            var id = (this.ReceivedEvent as Timer.TimeoutEvent).Timer;

            if (this.CommunicateLeaderTimeout.Equals(id))
            {
                this.Assert(this.CurrentLeader.Item1 <= this.MyRank, "this.CurrentLeader <= this.MyRank");
                this.SendEvent(this.ParentServer, new LeaderElection.NewLeader(this.CurrentLeader.Item2, this.CurrentLeader.Item1));
                this.CurrentLeader = Tuple.Create(this.MyRank, this.Id);
                this.SendEvent(this.CommunicateLeaderTimeout, new Timer.StartTimerEvent());
                this.SendEvent(this.BroadCastTimeout, new Timer.CancelTimerEvent());
            }
        }

        private void CalculateLeader()
        {
            var rank = (this.ReceivedEvent as LeaderElection.Ping).Rank;
            var leader = (this.ReceivedEvent as LeaderElection.Ping).LeaderElection;

            if (rank < this.MyRank)
            {
                this.CurrentLeader = Tuple.Create(rank, leader);
            }
        }
    }
}
