// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.Coyote;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Specifications;

namespace Coyote.Examples.Chord
{
    internal class LivenessMonitor : Monitor
    {
        public class NotifyClientRequest : Event
        {
            public int Key;

            public NotifyClientRequest(int key)
                : base()
            {
                this.Key = key;
            }
        }

        public class NotifyClientResponse : Event
        {
            public int Key;

            public NotifyClientResponse(int key)
                : base()
            {
                this.Key = key;
            }
        }

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        private class Init : State { }

        private Transition InitOnEntry()
        {
            return this.GotoState<Responded>();
        }

        [Cold]
        [OnEventGotoState(typeof(NotifyClientRequest), typeof(Requested))]
        private class Responded : State { }

        [Hot]
        [OnEventGotoState(typeof(NotifyClientResponse), typeof(Responded))]
        private class Requested : State { }
    }
}
