// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Coyote;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Specifications;

namespace Coyote.Examples.Raft
{
    internal class SafetyMonitor : Monitor
    {
        internal class NotifyLeaderElected : Event
        {
            public int Term;

            public NotifyLeaderElected(int term)
                : base()
            {
                this.Term = term;
            }
        }

        private class LocalEvent : Event { }

        private HashSet<int> TermsWithLeader;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(LocalEvent), typeof(Monitoring))]
        private class Init : State { }

        private Transition InitOnEntry()
        {
            this.TermsWithLeader = new HashSet<int>();
            return this.RaiseEvent(new LocalEvent());
        }

        [OnEventDoAction(typeof(NotifyLeaderElected), nameof(ProcessLeaderElected))]
        private class Monitoring : State { }

        private void ProcessLeaderElected(Event e)
        {
            var term = (e as NotifyLeaderElected).Term;

            this.Assert(!this.TermsWithLeader.Contains(term), "Detected more than one leader in term " + term);
            this.TermsWithLeader.Add(term);
        }
    }
}
