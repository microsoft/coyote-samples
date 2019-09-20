// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.Coyote.Machines;

namespace Coyote.Examples.MultiPaxos
{
    internal class Timer : Machine
    {
        internal class Config : Event
        {
            public MachineId Target;
            public int TimeoutValue;

            public Config(MachineId id, int value)
            {
                this.Target = id;
                this.TimeoutValue = value;
            }
        }

        internal class TimeoutEvent : Event
        {
            public MachineId Timer;

            public TimeoutEvent(MachineId id)
            {
                this.Timer = id;
            }
        }

        internal class StartTimerEvent : Event { }

        internal class CancelTimerEvent : Event { }

        private MachineId Target;
        private int TimeoutValue;

        [Start]
        [OnEventGotoState(typeof(Local), typeof(Loop))]
        [OnEventDoAction(typeof(Timer.Config), nameof(Configure))]
        private class Init : MachineState { }

        private void Configure()
        {
            this.Target = (this.ReceivedEvent as Timer.Config).Target;
            this.TimeoutValue = (this.ReceivedEvent as Timer.Config).TimeoutValue;
            this.Raise(new Local());
        }

        [OnEventGotoState(typeof(Timer.StartTimerEvent), typeof(TimerStarted))]
        [IgnoreEvents(typeof(Timer.CancelTimerEvent))]
        private class Loop : MachineState { }

        [OnEntry(nameof(TimerStartedOnEntry))]
        [OnEventGotoState(typeof(Local), typeof(Loop))]
        [OnEventGotoState(typeof(Timer.CancelTimerEvent), typeof(Loop))]
        [IgnoreEvents(typeof(Timer.StartTimerEvent))]
        private class TimerStarted : MachineState { }

        private void TimerStartedOnEntry()
        {
            if (this.Random())
            {
                this.Send(this.Target, new Timer.TimeoutEvent(this.Id));
                this.Raise(new Local());
            }
        }
    }
}
