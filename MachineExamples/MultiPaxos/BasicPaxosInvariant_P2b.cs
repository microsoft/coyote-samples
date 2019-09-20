// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Coyote;
using Microsoft.Coyote.Machines;

namespace Coyote.Examples.MultiPaxos
{
    internal class BasicPaxosInvariant_P2b : Monitor
    {
        internal class MonitorValueProposed : Event
        {
            public MachineId Node;
            public int NextSlotForProposer;
            public Tuple<int, int> NextProposal;
            public int ProposeVal;

            public MonitorValueProposed(MachineId id, int nextSlot, Tuple<int, int> nextProposal, int proposeVal)
            {
                this.Node = id;
                this.NextSlotForProposer = nextSlot;
                this.NextProposal = nextProposal;
                this.ProposeVal = proposeVal;
            }
        }

        internal class MonitorValueChosen : Event
        {
            public MachineId Node;
            public int NextSlotForProposer;
            public Tuple<int, int> NextProposal;
            public int ProposeVal;

            public MonitorValueChosen(MachineId id, int nextSlot, Tuple<int, int> nextProposal, int proposeVal)
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
        private class Init : MonitorState { }

        private void InitOnEntry()
        {
            this.LastValueChosen = new Dictionary<int, Tuple<int, int, int>>();
            this.Goto<WaitForValueChosen>();
        }

        [OnEventGotoState(typeof(BasicPaxosInvariant_P2b.MonitorValueChosen), typeof(CheckValueProposed), nameof(WaitForValueChosenAction))]
        [IgnoreEvents(typeof(BasicPaxosInvariant_P2b.MonitorValueProposed))]
        private class WaitForValueChosen : MonitorState { }

        private void WaitForValueChosenAction()
        {
            var slot = (this.ReceivedEvent as BasicPaxosInvariant_P2b.MonitorValueChosen).NextSlotForProposer;
            var proposal = Tuple.Create((this.ReceivedEvent as BasicPaxosInvariant_P2b.MonitorValueChosen).NextProposal.Item1,
                (this.ReceivedEvent as BasicPaxosInvariant_P2b.MonitorValueChosen).NextProposal.Item2,
                (this.ReceivedEvent as BasicPaxosInvariant_P2b.MonitorValueChosen).ProposeVal);
            this.LastValueChosen.Add(slot, proposal);
        }

        [OnEventGotoState(typeof(BasicPaxosInvariant_P2b.MonitorValueChosen), typeof(CheckValueProposed), nameof(ValueChosenAction))]
        [OnEventGotoState(typeof(BasicPaxosInvariant_P2b.MonitorValueProposed), typeof(CheckValueProposed), nameof(ValueProposedAction))]
        private class CheckValueProposed : MonitorState { }

        private void ValueChosenAction()
        {
            var slot = (this.ReceivedEvent as BasicPaxosInvariant_P2b.MonitorValueChosen).NextSlotForProposer;
            var proposal = Tuple.Create((this.ReceivedEvent as BasicPaxosInvariant_P2b.MonitorValueChosen).NextProposal.Item1,
                (this.ReceivedEvent as BasicPaxosInvariant_P2b.MonitorValueChosen).NextProposal.Item2,
                (this.ReceivedEvent as BasicPaxosInvariant_P2b.MonitorValueChosen).ProposeVal);

            this.Assert(this.LastValueChosen[slot].Item3 == proposal.Item3, "ValueChosenAction");
        }

        private void ValueProposedAction()
        {
            var slot = (this.ReceivedEvent as BasicPaxosInvariant_P2b.MonitorValueProposed).NextSlotForProposer;
            var proposal = Tuple.Create((this.ReceivedEvent as BasicPaxosInvariant_P2b.MonitorValueProposed).NextProposal.Item1,
                (this.ReceivedEvent as BasicPaxosInvariant_P2b.MonitorValueProposed).NextProposal.Item2,
                (this.ReceivedEvent as BasicPaxosInvariant_P2b.MonitorValueProposed).ProposeVal);

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
