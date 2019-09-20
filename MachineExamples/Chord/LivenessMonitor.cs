// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.Coyote;
using Microsoft.Coyote.Machines;

namespace Coyote.Examples.Chord
{
    internal class LivenessMonitor : Monitor
    {
        #region events

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

        #endregion

        #region states

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        private class Init : MonitorState { }

        private void InitOnEntry()
        {
            this.Goto<Responded>();
        }

        [Cold]
        [OnEventGotoState(typeof(NotifyClientRequest), typeof(Requested))]
        private class Responded : MonitorState { }

        [Hot]
        [OnEventGotoState(typeof(NotifyClientResponse), typeof(Responded))]
        private class Requested : MonitorState { }

        #endregion
    }
}
