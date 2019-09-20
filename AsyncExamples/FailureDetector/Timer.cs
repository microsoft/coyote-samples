// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.Coyote;
using Task = Microsoft.Coyote.Threading.Tasks.ControlledTask;

namespace Coyote.Examples.FailureDetector
{
    /// <summary>
    /// This is the call back interface you must implement when you use a timer.
    /// </summary>
    internal interface ITimerClient
    {
        Task OnTimeout();

        Task OnCancelSuccess();

        Task OnCancelFailure();
    }

    /// <summary>
    /// The Timer models the operating system timer.
    ///
    /// It fires timeouts in a non-deterministic fashion using the Coyote
    /// method 'Random', rather than using an actual timeout.
    /// </summary>
    internal class Timer
    {
        private enum TimerState
        {
            WaitForReq,
            WaitForCancel
        }

        private TimerState State;

        private int Timeout;

        /// <summary>
        /// Reference to the client of the timer.
        /// </summary>
        private readonly ITimerClient Target;

        public Timer(ITimerClient client)
        {
            this.Target = client;
            this.State = TimerState.WaitForReq;
        }

        /// <summary>
        /// Although this event accepts a timeout value, because
        /// this machine models a timer by nondeterministically
        /// triggering a timeout, this value is not used.
        /// </summary>
        internal async Task StartTimer(int timeout)
        {
            this.Timeout = timeout;
            await Task.Yield();
            if (this.State == TimerState.WaitForReq)
            {
                this.State = TimerState.WaitForCancel;
                _ = this.WaitForCancel();
            }
            else
            {
                // ignore start events if we are in this state.
            }
        }

        private async Task WaitForCancel()
        {
            // The async programming model doesn't have the concept of "Default" events.
            // So we invent the concept here.
            await Task.Delay(Specification.ChooseRandomInteger(10) + 1);
            if (this.State == TimerState.WaitForCancel)
            {
                this.State = TimerState.WaitForReq;
                // have not received a cancel, so this is a real timeout!
                await this.Target.OnTimeout();
            }
        }

        /// <summary>
        /// In the 'WaitForCancel' state, any 'StartTimerEvent' event is dropped without any
        /// action.
        /// </summary>
        internal async Task CancelTimer()
        {
            await Task.Delay(1);
            if (this.State == TimerState.WaitForCancel)
            {
                this.State = TimerState.WaitForReq;
                await this.Target.OnCancelFailure();
            }
            else
            {
                await this.CancelTimerAction();
            }
        }

        /// <summary>
        /// The response to a 'CancelTimer' event is nondeterministic. During testing, Coyote will
        /// take control of this source of nondeterminism and explore different execution paths.
        ///
        /// Using this approach, we model the race condition between the arrival of a 'CancelTimer'
        /// event from the target and the elapse of the timer.
        /// </summary>
        private async Task CancelTimerAction()
        {
            // A nondeterministic choice that is controlled by the Coyote runtime during testing.
            if (Specification.ChooseRandomBoolean())
            {
                // this.Send(this.Target, new CancelSuccess());
                await this.Target.OnCancelSuccess();
            }
            else
            {
                // this.Send(this.Target, new CancelFailure());
                await this.Target.OnCancelFailure();
                // this.Send(this.Target, new TimeoutEvent());
                await this.Target.OnTimeout();
            }
        }
    }
}
