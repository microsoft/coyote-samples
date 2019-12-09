// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.Coyote;
using Microsoft.Coyote.Actors;

namespace Coyote.Examples.BoundedAsync
{
    /// <summary>
    /// Scheduler machine that creates a user-defined number of 'Process' machines.
    /// </summary>
    internal class Scheduler : StateMachine
    {
        /// <summary>
        /// Event declaration of a 'Config' event that contains payload.
        /// </summary>
        internal class Config : Event
        {
            /// <summary>
            /// The payload of the event. It is the number of 'Process'
            /// machines to create.
            /// </summary>
            public int ProcessNum;

            public Config(int processNum)
            {
                this.ProcessNum = processNum;
            }
        }

        private class Unit : Event { }

        private ActorId[] Processes;

        private int Count;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        private class Init : State { }

        private Transition InitOnEntry(Event e)
        {
            // Receives a configuration event containing the number of 'Process'
            // machines to create.
            int processNum = (e as Config).ProcessNum;

            // Assert that >= 3 'Process' machines will be created.
            this.Assert(processNum >= 3, "The number of 'Process' machines to spawn must be >= 3.");

            // Creates the 'Process' machines.
            this.Processes = new ActorId[processNum];
            for (int i = 0; i < processNum; i++)
            {
                this.Processes[i] = this.CreateActor(typeof(Process), new Process.Configure(this.Id));
            }

            for (int i = 0; i < processNum; i++)
            {
                int left, right;

                // Wrap left.
                if (i == 0)
                {
                    left = processNum - 1;
                }
                else
                {
                    left = i - 1;
                }

                // Wrap right.
                if (i == processNum - 1)
                {
                    right = 0;
                }
                else
                {
                    right = i + 1;
                }

                // Sends the machine a reference to its left and right neighbour machines.
                this.SendEvent(this.Processes[i], new Process.Initialize(this.Processes[left], this.Processes[right]));
            }

            this.Count = 0;

            // Transition to the 'Sync' state in the end of this action.
            return this.GotoState<Sync>();
        }

        /// <summary>
        /// This state declares an 'OnExit' action, which executes
        /// when the machine transitions out of this state.
        ///
        /// When the machine has received a 'Req' event from each
        /// 'Process' machine, it loops back to this state (after
        /// executing the 'OnExit' action).
        /// </summary>
        [OnExit(nameof(ActiveOnExit))]
        [OnEventDoAction(typeof(Process.Req), nameof(CountReq))]
        [OnEventGotoState(typeof(Unit), typeof(Sync))]
        private class Sync : State { }

        private void ActiveOnExit()
        {
            // When all 'Process' machines have synced, the scheduler sends
            // a 'Resp' event to each one of them.
            for (int i = 0; i < this.Processes.Length; i++)
            {
                this.SendEvent(this.Processes[i], new Process.Resp());
            }
        }

        private Transition CountReq()
        {
            this.Count++;

            // Checks if all processes have responded.
            if (this.Count == this.Processes.Length)
            {
                this.Count = 0;
                return this.RaiseEvent(new Unit());
            }

            return default;
        }
    }
}
