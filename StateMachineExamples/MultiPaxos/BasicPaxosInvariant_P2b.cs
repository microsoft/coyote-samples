// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Coyote;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Specifications;

namespace Coyote.Examples.MultiPaxos
{
    internal class BasicPaxosInvariant_P2b : Monitor
    {
        internal class MonitorValueProposed : Event
        {
            public ActorId Node;
            public int NextSlotForProposer;
            public Tuple<int, int> NextProposal;
            public int ProposeVal;

            public MonitorValueProposed(ActorId id, int nextSlot, Tuple<int, int> nextProposal, int proposeVal)
            {
                this.Node = id;
                this.NextSlotForProposer = nextSlot;
                this.NextProposal = nextProposal;
                this.ProposeVal = proposeVal;
            }
        }

        internal class MonitorValueChosen : Event
        {
            public ActorId Node;
            public int NextSlotForProposer;
            public Tuple<int, int> NextProposal;
            public int ProposeVal;

            public MonitorValueChosen(ActorId id, int nextSlot, Tuple<int, int> nextProposal, int proposeVal)
            {
                this.Node = id;
                this.NextSlotForProposer = nextSlot;
                this.NextProposal = nextProposal;
                this.ProposeVal = proposeVal;
            }
        }

        private Dictionary<int, Tuple<int, int, int>> LastValueChosen;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        private class Init : State { }

        private Transition InitOnEntry()
        {
            this.LastValueChosen = new Dictionary<int, Tuple<int, int, int>>();
            return this.GotoState<WaitForValueChosen>();
        }

        [OnEventGotoState(typeof(MonitorValueChosen), typeof(CheckValueProposed), nameof(WaitForValueChosenAction))]
        [IgnoreEvents(typeof(MonitorValueProposed))]
        private class WaitForValueChosen : State { }

        private void WaitForValueChosenAction(Event e)
        {
            var slot = (e as MonitorValueChosen).NextSlotForProposer;
            var proposal = Tuple.Create((e as MonitorValueChosen).NextProposal.Item1,
                (e as MonitorValueChosen).NextProposal.Item2,
                (e as MonitorValueChosen).ProposeVal);
            this.LastValueChosen.Add(slot, proposal);
        }

        [OnEventGotoState(typeof(MonitorValueChosen), typeof(CheckValueProposed), nameof(ValueChosenAction))]
        [OnEventGotoState(typeof(MonitorValueProposed), typeof(CheckValueProposed), nameof(ValueProposedAction))]
        private class CheckValueProposed : State { }

        private void ValueChosenAction(Event e)
        {
            var slot = (e as MonitorValueChosen).NextSlotForProposer;
            var proposal = Tuple.Create((e as MonitorValueChosen).NextProposal.Item1,
                (e as MonitorValueChosen).NextProposal.Item2,
                (e as MonitorValueChosen).ProposeVal);

            this.Assert(this.LastValueChosen[slot].Item3 == proposal.Item3, "ValueChosenAction");
        }

        private void ValueProposedAction(Event e)
        {
            var slot = (e as MonitorValueProposed).NextSlotForProposer;
            var proposal = Tuple.Create((e as MonitorValueProposed).NextProposal.Item1,
                (e as MonitorValueProposed).NextProposal.Item2,
                (e as MonitorValueProposed).ProposeVal);

            if (LessThan(this.LastValueChosen[slot].Item1, this.LastValueChosen[slot].Item2,
                proposal.Item1, proposal.Item2))
            {
                this.Assert(this.LastValueChosen[slot].Item3 == proposal.Item3, "ValueProposedAction");
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
