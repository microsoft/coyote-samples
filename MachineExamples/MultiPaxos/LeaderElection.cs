// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Coyote.Machines;

namespace Coyote.Examples.MultiPaxos
{
    internal class LeaderElection : Machine
    {
        internal class Config : Event
        {
            public List<MachineId> Servers;
            public MachineId ParentServer;
            public int MyRank;

            public Config(List<MachineId> servers, MachineId parentServer, int myRank)
            {
                this.Servers = servers;
                this.ParentServer = parentServer;
                this.MyRank = myRank;
            }
        }

        internal class Ping : Event
        {
            public MachineId LeaderElection;
            public int Rank;

            public Ping(MachineId leaderElection, int rank)
            {
                this.LeaderElection = leaderElection;
                this.Rank = rank;
            }
        }

        internal class NewLeader : Event
        {
            public MachineId CurrentLeader;
            public int Rank;

            public NewLeader(MachineId leader, int rank)
            {
                this.CurrentLeader = leader;
                this.Rank = rank;
            }
        }

        private List<MachineId> Servers;
        private MachineId ParentServer;
        private int MyRank;
        private Tuple<int, MachineId> CurrentLeader;
        private MachineId CommunicateLeaderTimeout;
        private MachineId BroadCastTimeout;

        [Start]
        [OnEventGotoState(typeof(Local), typeof(ProcessPings))]
        [OnEventDoAction(typeof(LeaderElection.Config), nameof(Configure))]
        private class Init : MachineState { }

        private void Configure()
        {
            this.Servers = (this.ReceivedEvent as LeaderElection.Config).Servers;
            this.ParentServer = (this.ReceivedEvent as LeaderElection.Config).ParentServer;
            this.MyRank = (this.ReceivedEvent as LeaderElection.Config).MyRank;

            this.CurrentLeader = Tuple.Create(this.MyRank, this.Id);

            this.CommunicateLeaderTimeout = this.CreateMachine(typeof(Timer));
            this.Send(this.CommunicateLeaderTimeout, new Timer.Config(this.Id, 100));

            this.BroadCastTimeout = this.CreateMachine(typeof(Timer));
            this.Send(this.BroadCastTimeout, new Timer.Config(this.Id, 10));

            this.Raise(new Local());
        }

        [OnEntry(nameof(ProcessPingsOnEntry))]
        [OnEventGotoState(typeof(Timer.TimeoutEvent), typeof(ProcessPings), nameof(ProcessPingsAction))]
        [OnEventDoAction(typeof(LeaderElection.Ping), nameof(CalculateLeader))]
        private class ProcessPings : MachineState { }

        private void ProcessPingsOnEntry()
        {
            foreach (var server in this.Servers)
            {
                this.Send(server, new LeaderElection.Ping(this.Id, this.MyRank));
            }

            this.Send(this.BroadCastTimeout, new Timer.StartTimerEvent());
        }

        private void ProcessPingsAction()
        {
            var id = (this.ReceivedEvent as Timer.TimeoutEvent).Timer;

            if (this.CommunicateLeaderTimeout.Equals(id))
            {
                this.Assert(this.CurrentLeader.Item1 <= this.MyRank, "this.CurrentLeader <= this.MyRank");
                this.Send(this.ParentServer, new LeaderElection.NewLeader(this.CurrentLeader.Item2, this.CurrentLeader.Item1));
                this.CurrentLeader = Tuple.Create(this.MyRank, this.Id);
                this.Send(this.CommunicateLeaderTimeout, new Timer.StartTimerEvent());
                this.Send(this.BroadCastTimeout, new Timer.CancelTimerEvent());
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
