// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.Coyote;
using Microsoft.Coyote.Actors;

namespace Coyote.Examples.MultiPaxos
{
    internal class Timer : StateMachine
    {
        internal class Config : Event
        {
            public ActorId Target;
            public int TimeoutValue;

            public Config(ActorId id, int value)
            {
                this.Target = id;
                this.TimeoutValue = value;
            }
        }

        internal class TimeoutEvent : Event
        {
            public ActorId Timer;

            public TimeoutEvent(ActorId id)
            {
                this.Timer = id;
            }
        }

        internal class StartTimerEvent : Event { }

        internal class CancelTimerEvent : Event { }

        private ActorId Target;
        private int TimeoutValue;

        [Start]
        [OnEventGotoState(typeof(Local), typeof(Loop))]
        [OnEventDoAction(typeof(Timer.Config), nameof(Configure))]
        private class Init : State { }

        private void Configure()
        {
            this.Target = (this.ReceivedEvent as Timer.Config).Target;
            this.TimeoutValue = (this.ReceivedEvent as Timer.Config).TimeoutValue;
            this.RaiseEvent(new Local());
        }

        [OnEventGotoState(typeof(Timer.StartTimerEvent), typeof(TimerStarted))]
        [IgnoreEvents(typeof(Timer.CancelTimerEvent))]
        private class Loop : State { }

        [OnEntry(nameof(TimerStartedOnEntry))]
        [OnEventGotoState(typeof(Local), typeof(Loop))]
        [OnEventGotoState(typeof(Timer.CancelTimerEvent), typeof(Loop))]
        [IgnoreEvents(typeof(Timer.StartTimerEvent))]
        private class TimerStarted : State { }

        private void TimerStartedOnEntry()
        {
            if (this.Random())
            {
                this.SendEvent(this.Target, new Timer.TimeoutEvent(this.Id));
                this.RaiseEvent(new Local());
            }
        }
    }
}
