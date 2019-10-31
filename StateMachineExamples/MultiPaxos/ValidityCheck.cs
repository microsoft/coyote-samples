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
    internal class ValidityCheck : Monitor
    {
        internal class MonitorClientSent : Event
        {
            public int Request;

            public MonitorClientSent(int req)
            {
                this.Request = req;
            }
        }

        internal class MonitorProposerSent : Event
        {
            public int ProposeVal;

            public MonitorProposerSent(int val)
            {
                this.ProposeVal = val;
            }
        }

        internal class MonitorProposerChosen : Event
        {
            public int ChosenVal;

            public MonitorProposerChosen(int val)
            {
                this.ChosenVal = val;
            }
        }

        private Dictionary<int, int> ClientSet;
        private Dictionary<int, int> ProposedSet;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        private class Init : State { }

        private void InitOnEntry()
        {
            this.ClientSet = new Dictionary<int, int>();
            this.ProposedSet = new Dictionary<int, int>();
            this.Goto<Wait>();
        }

        [OnEventDoAction(typeof(ValidityCheck.MonitorClientSent), nameof(AddClientSet))]
        [OnEventDoAction(typeof(ValidityCheck.MonitorProposerSent), nameof(AddProposerSet))]
        [OnEventDoAction(typeof(ValidityCheck.MonitorProposerChosen), nameof(CheckChosenValmachineity))]
        private class Wait : State { }

        private void AddClientSet()
        {
            var index = (this.ReceivedEvent as ValidityCheck.MonitorClientSent).Request;
            this.ClientSet.Add(index, 0);
        }

        private void AddProposerSet()
        {
            var index = (this.ReceivedEvent as ValidityCheck.MonitorProposerSent).ProposeVal;
            this.Assert(this.ClientSet.ContainsKey(index), "Client set does not contain {0}", index);

            if (this.ProposedSet.ContainsKey(index))
            {
                this.ProposedSet[index] = 0;
            }
            else
            {
                this.ProposedSet.Add(index, 0);
            }
        }

        private void CheckChosenValmachineity()
        {
            var index = (this.ReceivedEvent as ValidityCheck.MonitorProposerChosen).ChosenVal;
            this.Assert(this.ProposedSet.ContainsKey(index), "Proposed set does not contain {0}", index);
        }
    }
}
