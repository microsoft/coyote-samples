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
    internal class PaxosNode : StateMachine
    {
        internal class Config : Event
        {
            public int MyRank;

            public Config(int id)
            {
                this.MyRank = id;
            }
        }

        internal class AllNodes : Event
        {
            public List<ActorId> Nodes;

            public AllNodes(List<ActorId> nodes)
            {
                this.Nodes = nodes;
            }
        }

        internal class Prepare : Event
        {
            public ActorId Node;
            public int NextSlotForProposer;
            public Tuple<int, int> NextProposal;
            public int MyRank;

            public Prepare(ActorId id, int nextSlot, Tuple<int, int> nextProposal, int myRank)
            {
                this.Node = id;
                this.NextSlotForProposer = nextSlot;
                this.NextProposal = nextProposal;
                this.MyRank = myRank;
            }
        }

        internal class Accepted : Event
        {
            public int Slot;
            public int Round;
            public int Server;
            public int Value;

            public Accepted(int slot, int round, int server, int value)
            {
                this.Slot = slot;
                this.Round = round;
                this.Server = server;
                this.Value = value;
            }
        }

        internal class Chosen : Event
        {
            public int Slot;
            public int Round;
            public int Server;
            public int Value;

            public Chosen(int slot, int round, int server, int value)
            {
                this.Slot = slot;
                this.Round = round;
                this.Server = server;
                this.Value = value;
            }
        }

        internal class Agree : Event
        {
            public int Slot;
            public int Round;
            public int Server;
            public int Value;

            public Agree(int slot, int round, int server, int value)
            {
                this.Slot = slot;
                this.Round = round;
                this.Server = server;
                this.Value = value;
            }
        }

        internal class Accept : Event
        {
            public ActorId Node;
            public int NextSlotForProposer;
            public Tuple<int, int> NextProposal;
            public int ProposeVal;

            public Accept(ActorId id, int nextSlot, Tuple<int, int> nextProposal, int proposeVal)
            {
                this.Node = id;
                this.NextSlotForProposer = nextSlot;
                this.NextProposal = nextProposal;
                this.ProposeVal = proposeVal;
            }
        }

        internal class Reject : Event
        {
            public int Round;
            public Tuple<int, int> Proposal;

            public Reject(int round, Tuple<int, int> proposal)
            {
                this.Round = round;
                this.Proposal = proposal;
            }
        }

        internal class Update : Event
        {
            public int V1;
            public int V2;

            public Update(int v1, int v2)
            {
                this.V1 = v1;
                this.V2 = v2;
            }
        }

        private Tuple<int, ActorId> CurrentLeader;
        private ActorId LeaderElectionService;

        private List<ActorId> Acceptors;
        private int CommitValue;
        private int ProposeVal;
        private int Majority;
        private int MyRank;
        private Tuple<int, int> NextProposal;
        private Tuple<int, int, int> ReceivedAgree;
        private int MaxRound;
        private int AcceptCount;
        private int AgreeCount;
        private ActorId Timer;
        private int NextSlotForProposer;

        private Dictionary<int, Tuple<int, int, int>> AcceptorSlots;

        private Dictionary<int, Tuple<int, int, int>> LearnerSlots;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(Local), typeof(PerformOperation))]
        [OnEventDoAction(typeof(Config), nameof(Configure))]
        [OnEventDoAction(typeof(AllNodes), nameof(UpdateAcceptors))]
        [DeferEvents(typeof(LeaderElection.Ping))]
        private class Init : State { }

        private void InitOnEntry()
        {
            this.Acceptors = new List<ActorId>();
            this.AcceptorSlots = new Dictionary<int, Tuple<int, int, int>>();
            this.LearnerSlots = new Dictionary<int, Tuple<int, int, int>>();
        }

        private void Configure(Event e)
        {
            this.MyRank = (e as Config).MyRank;

            this.CurrentLeader = Tuple.Create(this.MyRank, this.Id);
            this.MaxRound = 0;

            this.Timer = this.CreateActor(typeof(Timer));
            this.SendEvent(this.Timer, new Timer.Config(this.Id, 10));

            this.NextSlotForProposer = 0;
        }

        [OnEventPushState(typeof(GoPropose), typeof(ProposeValuePhase1))]
        [OnEventPushState(typeof(Chosen), typeof(RunLearner))]
        [OnEventDoAction(typeof(Update), nameof(CheckIfLeader))]
        [OnEventDoAction(typeof(Prepare), nameof(PrepareAction))]
        [OnEventDoAction(typeof(Accept), nameof(AcceptAction))]
        [OnEventDoAction(typeof(LeaderElection.Ping), nameof(ForwardToLE))]
        [OnEventDoAction(typeof(LeaderElection.NewLeader), nameof(UpdateLeader))]
        [IgnoreEvents(typeof(Agree), typeof(Accepted), typeof(Timer.TimeoutEvent), typeof(Reject))]
        private class PerformOperation : State { }

        [OnEntry(nameof(ProposeValuePhase1OnEntry))]
        [OnEventGotoState(typeof(Reject), typeof(ProposeValuePhase1), nameof(ProposeValuePhase1RejectAction))]
        [OnEventGotoState(typeof(Success), typeof(ProposeValuePhase2), nameof(ProposeValuePhase1SuccessAction))]
        [OnEventGotoState(typeof(Timer.TimeoutEvent), typeof(ProposeValuePhase1))]
        [OnEventDoAction(typeof(Agree), nameof(CountAgree))]
        [IgnoreEvents(typeof(Accepted))]
        private class ProposeValuePhase1 : State { }

        private void ProposeValuePhase1OnEntry()
        {
            this.AgreeCount = 0;
            this.NextProposal = this.GetNextProposal(this.MaxRound);
            this.ReceivedAgree = Tuple.Create(-1, -1, -1);

            foreach (var acceptor in this.Acceptors)
            {
                this.SendEvent(acceptor, new Prepare(this.Id, this.NextSlotForProposer, this.NextProposal, this.MyRank));
            }

            this.Monitor<ValidityCheck>(new ValidityCheck.MonitorProposerSent(this.ProposeVal));
            this.SendEvent(this.Timer, new Timer.StartTimerEvent());
        }

        private void ProposeValuePhase1RejectAction(Event e)
        {
            var round = (e as Reject).Round;

            if (this.NextProposal.Item1 <= round)
            {
                this.MaxRound = round;
            }

            this.SendEvent(this.Timer, new Timer.CancelTimerEvent());
        }

        private void ProposeValuePhase1SuccessAction()
        {
            this.SendEvent(this.Timer, new Timer.CancelTimerEvent());
        }

        [OnEntry(nameof(ProposeValuePhase2OnEntry))]
        [OnExit(nameof(ProposeValuePhase2OnExit))]
        [OnEventGotoState(typeof(Reject), typeof(ProposeValuePhase1), nameof(ProposeValuePhase2RejectAction))]
        [OnEventGotoState(typeof(Timer.TimeoutEvent), typeof(ProposeValuePhase1))]
        [OnEventDoAction(typeof(Accepted), nameof(CountAccepted))]
        [IgnoreEvents(typeof(Agree))]
        private class ProposeValuePhase2 : State { }

        private void ProposeValuePhase2OnEntry()
        {
            this.AcceptCount = 0;
            this.ProposeVal = this.GetHighestProposedValue();

            this.Monitor<BasicPaxosInvariant_P2b>(new BasicPaxosInvariant_P2b.MonitorValueProposed(
                this.Id, this.NextSlotForProposer, this.NextProposal, this.ProposeVal));
            this.Monitor<ValidityCheck>(new ValidityCheck.MonitorProposerSent(this.ProposeVal));

            foreach (var acceptor in this.Acceptors)
            {
                this.SendEvent(acceptor, new Accept(this.Id, this.NextSlotForProposer, this.NextProposal, this.ProposeVal));
            }

            this.SendEvent(this.Timer, new Timer.StartTimerEvent());
        }

        private void ProposeValuePhase2OnExit(Event e)
        {
            if (e.GetType() == typeof(Chosen))
            {
                this.Monitor<BasicPaxosInvariant_P2b>(new BasicPaxosInvariant_P2b.MonitorValueChosen(
                    this.Id, this.NextSlotForProposer, this.NextProposal, this.ProposeVal));

                this.SendEvent(this.Timer, new Timer.CancelTimerEvent());

                this.Monitor<ValidityCheck>(new ValidityCheck.MonitorProposerChosen(this.ProposeVal));

                this.NextSlotForProposer++;
            }
        }

        private void ProposeValuePhase2RejectAction(Event e)
        {
            var round = (e as Reject).Round;

            if (this.NextProposal.Item1 <= round)
            {
                this.MaxRound = round;
            }

            this.SendEvent(this.Timer, new Timer.CancelTimerEvent());
        }

        [OnEntry(nameof(RunLearnerOnEntry))]
        [IgnoreEvents(typeof(Agree), typeof(Accepted), typeof(Timer.TimeoutEvent),
            typeof(Prepare), typeof(Reject), typeof(Accept))]
        [DeferEvents(typeof(LeaderElection.NewLeader))]
        private class RunLearner : State { }

        private Transition RunLearnerOnEntry(Event e)
        {
            var slot = (e as Chosen).Slot;
            var round = (e as Chosen).Round;
            var server = (e as Chosen).Server;
            var value = (e as Chosen).Value;

            this.LearnerSlots[slot] = Tuple.Create(round, server, value);

            if (this.CommitValue == value)
            {
                return this.PopState();
            }

            this.ProposeVal = this.CommitValue;
            return this.RaiseEvent(new GoPropose());
        }

        private Transition UpdateAcceptors(Event e)
        {
            var acceptors = (e as AllNodes).Nodes;

            this.Acceptors = acceptors;

            this.Majority = (this.Acceptors.Count / 2) + 1;
            this.Assert(this.Majority == 2, "Majority is not 2");

            this.LeaderElectionService = this.CreateActor(typeof(LeaderElection));
            this.SendEvent(this.LeaderElectionService, new LeaderElection.Config(this.Acceptors, this.Id, this.MyRank));

            return this.RaiseEvent(new Local());
        }

        private Transition CheckIfLeader(Event e)
        {
            var updateEvent = e as Update;
            if (this.CurrentLeader.Item1 == this.MyRank)
            {
                this.CommitValue = updateEvent.V2;
                this.ProposeVal = this.CommitValue;
                return this.RaiseEvent(new GoPropose());
            }

            this.SendEvent(this.CurrentLeader.Item2, new Update(updateEvent.V1, updateEvent.V2));
            return default;
        }

        private void PrepareAction(Event e)
        {
            var proposer = (e as Prepare).Node;
            var slot = (e as Prepare).NextSlotForProposer;
            var round = (e as Prepare).NextProposal.Item1;
            var server = (e as Prepare).NextProposal.Item2;

            if (!this.AcceptorSlots.ContainsKey(slot))
            {
                this.SendEvent(proposer, new Agree(slot, -1, -1, -1));
                return;
            }

            if (LessThan(round, server, this.AcceptorSlots[slot].Item1, this.AcceptorSlots[slot].Item2))
            {
                this.SendEvent(proposer, new Reject(slot, Tuple.Create(this.AcceptorSlots[slot].Item1,
                    this.AcceptorSlots[slot].Item2)));
            }
            else
            {
                this.SendEvent(proposer, new Agree(slot, this.AcceptorSlots[slot].Item1,
                    this.AcceptorSlots[slot].Item2, this.AcceptorSlots[slot].Item3));
                this.AcceptorSlots[slot] = Tuple.Create(this.AcceptorSlots[slot].Item1, this.AcceptorSlots[slot].Item2, -1);
            }
        }

        private void AcceptAction(Event e)
        {
            var acceptEvent = e as Accept;

            var proposer = acceptEvent.Node;
            var slot = acceptEvent.NextSlotForProposer;
            var round = acceptEvent.NextProposal.Item1;
            var server = acceptEvent.NextProposal.Item2;
            var value = acceptEvent.ProposeVal;

            if (this.AcceptorSlots.ContainsKey(slot))
            {
                if (!IsEqual(round, server, this.AcceptorSlots[slot].Item1, this.AcceptorSlots[slot].Item2))
                {
                    this.SendEvent(proposer, new Reject(slot, Tuple.Create(this.AcceptorSlots[slot].Item1,
                        this.AcceptorSlots[slot].Item2)));
                }
                else
                {
                    this.AcceptorSlots[slot] = Tuple.Create(round, server, value);
                    this.SendEvent(proposer, new Accepted(slot, round, server, value));
                }
            }
        }

        private void ForwardToLE(Event e)
        {
            this.SendEvent(this.LeaderElectionService, e);
        }

        private void UpdateLeader(Event e)
        {
            var newLeaderEvent = e as LeaderElection.NewLeader;
            this.CurrentLeader = Tuple.Create(newLeaderEvent.Rank, newLeaderEvent.CurrentLeader);
        }

        private Transition CountAgree(Event e)
        {
            var slot = (e as Agree).Slot;
            var round = (e as Agree).Round;
            var server = (e as Agree).Server;
            var value = (e as Agree).Value;

            if (slot == this.NextSlotForProposer)
            {
                this.AgreeCount++;
                if (LessThan(this.ReceivedAgree.Item1, this.ReceivedAgree.Item2, round, server))
                {
                    this.ReceivedAgree = Tuple.Create(round, server, value);
                }

                if (this.AgreeCount == this.Majority)
                {
                    return this.RaiseEvent(new Success());
                }
            }

            return default;
        }

        private Transition CountAccepted(Event e)
        {
            var acceptedEvent = e as Accepted;

            var slot = acceptedEvent.Slot;
            var round = acceptedEvent.Round;
            var server = acceptedEvent.Server;

            if (slot == this.NextSlotForProposer)
            {
                if (IsEqual(round, server, this.NextProposal.Item1, this.NextProposal.Item2))
                {
                    this.AcceptCount++;
                }

                if (this.AcceptCount == this.Majority)
                {
                    return this.RaiseEvent(new Chosen(acceptedEvent.Slot, acceptedEvent.Round,
                        acceptedEvent.Server, acceptedEvent.Value));
                }
            }

            return default;
        }

        private int GetHighestProposedValue()
        {
            if (this.ReceivedAgree.Item2 != -1)
            {
                return this.ReceivedAgree.Item2;
            }
            else
            {
                return this.CommitValue;
            }
        }

        private Tuple<int, int> GetNextProposal(int maxRound)
        {
            return Tuple.Create(maxRound + 1, this.MyRank);
        }

        private static bool IsEqual(int round1, int server1, int round2, int server2)
        {
            if (round1 == round2 && server1 == server2)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool LessThan(int round1, int server1, int round2, int server2)
        {
            if (round1 < round2)
            {
                return true;
            }
            else if (round1 == round2)
            {
                if (server1 < server2)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }
}
