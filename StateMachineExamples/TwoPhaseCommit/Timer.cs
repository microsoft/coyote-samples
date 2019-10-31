// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.Coyote;
using Microsoft.Coyote.Actors;

namespace Coyote.Examples.TwoPhaseCommit
{
    internal class Timer : StateMachine
    {
        internal class Config : Event
        {
            public ActorId Target;

            public Config(ActorId id)
                : base()
            {
                this.Target = id;
            }
        }

        internal class StartTimerEvent : Event
        {
            public int Timeout;

            public StartTimerEvent(int timeout)
                : base()
            {
                this.Timeout = timeout;
            }
        }

        internal class TimeoutEvent : Event { }

        internal class CancelTimerEvent : Event { }

        internal class CancelTimerSuccess : Event { }

        internal class CancelTimerFailure : Event { }

        private class Unit : Event { }

        private ActorId Target;

        [Start]
        [OnEventGotoState(typeof(Unit), typeof(Loop))]
        [OnEventDoAction(typeof(Config), nameof(Configure))]
        private class Init : State { }

        private void Configure()
        {
            this.Target = (this.ReceivedEvent as Config).Target;
            this.RaiseEvent(new Unit());
        }

        [OnEventGotoState(typeof(StartTimerEvent), typeof(TimerStarted))]
        [IgnoreEvents(typeof(CancelTimerEvent))]
        private class Loop : State { }

        [OnEntry(nameof(TimerStartedOnEntry))]
        [OnEventGotoState(typeof(Unit), typeof(Loop))]
        [OnEventDoAction(typeof(CancelTimerEvent), nameof(CancelingTimer))]
        private class TimerStarted : State { }

        private void TimerStartedOnEntry()
        {
            if (this.Random())
            {
                this.SendEvent(this.Target, new Timer.TimeoutEvent());
                this.RaiseEvent(new Unit());
            }
        }

        private void CancelingTimer()
        {
            if (this.Random())
            {
                this.SendEvent(this.Target, new CancelTimerFailure());
                this.SendEvent(this.Target, new TimeoutEvent());
            }
            else
            {
                this.SendEvent(this.Target, new CancelTimerSuccess());
            }

            this.RaiseEvent(new Unit());
        }
    }
}
