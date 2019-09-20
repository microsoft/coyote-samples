// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Coyote;
using Microsoft.Coyote.Machines;

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
        private class Init : MonitorState { }

        private void InitOnEntry()
        {
            this.TermsWithLeader = new HashSet<int>();
            this.Raise(new LocalEvent());
        }

        [OnEventDoAction(typeof(NotifyLeaderElected), nameof(ProcessLeaderElected))]
        private class Monitoring : MonitorState { }

        private void ProcessLeaderElected()
        {
            var term = (this.ReceivedEvent as NotifyLeaderElected).Term;

            this.Assert(!this.TermsWithLeader.Contains(term), "Detected more than one leader in term " + term);
            this.TermsWithLeader.Add(term);
        }
    }
}
