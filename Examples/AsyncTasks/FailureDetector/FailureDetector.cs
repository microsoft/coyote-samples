// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Specifications;
using Task = Microsoft.Coyote.Threading.Tasks.ControlledTask;

namespace Coyote.Examples.FailureDetector
{
    internal interface IFailureDetectorClient
    {
        Task OnNodeFailed(Node node);
    }

    /// <summary>
    /// Implementation of a failure detector Coyote state machine using the Coyote
    /// asynchronous tasks programming model.
    /// </summary>
    internal class FailureDetector : INodeClient, ITimerClient
    {
        // We really need states and a stack of states for this machine. Note that
        // managing states and a stack of states is done for you if you use the
        // State Machine programming model.
        private enum FailureDetectorStates
        {
            Init,
            SendPing,
            WaitForCancelResponse,
            Reset
        }

        private FailureDetectorStates State = FailureDetectorStates.Init;
        private readonly Stack<FailureDetectorStates> Stack = new Stack<FailureDetectorStates>();
        private Node PongDeferred;
        private bool TimeoutDeferred;

        private readonly ILogger Logger;

        public FailureDetector(IEnumerable<Node> nodes, ILogger logger)
        {
            this.Nodes = new HashSet<Node>(nodes);
            this.Logger = logger;
            this.Clients = new HashSet<IFailureDetectorClient>();
            this.Alive = new HashSet<Node>();
            this.Responses = new HashSet<Node>();
        }

        /// <summary>
        /// Nodes to be monitored.
        /// </summary>
        private readonly HashSet<Node> Nodes;

        /// <summary>
        /// Set of registered clients.
        /// </summary>
        private readonly HashSet<IFailureDetectorClient> Clients;

        /// <summary>
        /// Number of made 'Ping' attempts.
        /// </summary>
        private int Attempts;

        /// <summary>
        /// Set of alive nodes.
        /// </summary>
        private readonly HashSet<Node> Alive;

        /// <summary>
        /// Collected responses in one round.
        /// </summary>
        private readonly HashSet<Node> Responses;

        /// <summary>
        /// Reference to the timer machine.
        /// </summary>
        private Timer Timer;

        public async Task Init()
        {
            this.Alive.Clear();
            this.Responses.Clear();

            // Initializes the alive set to contain all available nodes.
            foreach (var node in this.Nodes)
            {
                this.Alive.Add(node);
            }

            // Initializes the timer.
            this.Timer = new Timer(this);

            // Transitions to the 'SendPing' state after everything has initialized.
            await this.PushState(FailureDetectorStates.SendPing);
        }

        private async Task PushState(FailureDetectorStates state)
        {
            this.Stack.Push(this.State);
            await this.EnterState(state);
        }

        private async Task Goto(FailureDetectorStates state)
        {
            await this.EnterState(state);
        }

        private async Task EnterState(FailureDetectorStates state)
        {
            this.Logger.WriteLine("FailureDetector entering {0} state", state);

            // Note that this sort of switch statement is building a state machine and
            // you will see in the MachineExamples that this can be done using custom
            // attributes from the State Machine Programming model like this:
            //
            // [OnEntry(nameof(SendPingOnEntry))]
            // [OnEventGotoState(typeof(RoundDone), typeof(Reset))]
            // [OnEventPushState(typeof(TimerCancelled), typeof(WaitForCancelResponse))]
            // [OnEventDoAction(typeof(Node.Pong), nameof(PongAction))]
            // [OnEventDoAction(typeof(Timer.TimeoutEvent), nameof(TimeoutAction))]
            this.State = state;
            switch (state)
            {
                case FailureDetectorStates.Init:
                    await this.Init();
                    break;
                case FailureDetectorStates.SendPing:
                    await this.SendPingOnEntry();
                    break;
                case FailureDetectorStates.WaitForCancelResponse:
                    break;
                case FailureDetectorStates.Reset:
                    await this.ResetOnEntry();
                    break;
            }
        }

        private async Task PopState()
        {
            if (this.Stack.Count == 0)
            {
                throw new Exception("Stack is empty!");
            }

            await this.EnterState(this.Stack.Pop());
        }

        internal void RegisterClient(IFailureDetectorClient client)
        {
            this.Clients.Add(client);
        }

        private void UnregisterClientAction(IFailureDetectorClient client)
        {
            if (this.Clients.Contains(client))
            {
                this.Clients.Remove(client);
            }
        }

        private async Task SendPingOnEntry()
        {
            foreach (var node in this.Nodes)
            {
                // Sends a 'Ping' event to any machine that has not responded.
                if (this.Alive.Contains(node) && !this.Responses.Contains(node))
                {
                    Specification.Monitor<Safety>(new Safety.Ping(node));
                    _ = node.Ping(this);
                }
            }

            // Starts the timer with a given timeout value. Note that in this sample,
            // the timeout value is not actually used, because the timer is abstracted
            // away using Coyote to enable systematic testing (i.e. timeouts are triggered
            // nondeterministically). In production, this model timer machine will be
            // replaced by a real timer.
            _ = this.Timer.StartTimer(100);

            // Handle any deferred events
            Node pong = this.PongDeferred;
            this.PongDeferred = null;
            if (pong != null)
            {
                await this.PongAction(pong);
            }

            if (this.TimeoutDeferred)
            {
                this.TimeoutDeferred = false;
                await this.TimeoutAction();
            }
        }

        /// <summary>
        /// This action is triggered whenever a node replies with a 'Pong' event.
        /// </summary>
        private async Task PongAction(Node node)
        {
            lock (this.Alive)
            {
                if (this.Alive.Contains(node))
                {
                    this.Responses.Add(node);

                    this.Logger.WriteLine("FailureDetector now has {0} responses", this.Responses.Count);
                }
            }

            // Checks if the status of alive nodes has changed.
            if (this.Responses.Count == this.Alive.Count)
            {
                this.Logger.WriteLine("FailureDetector sending CancelTimer in State {0}", this.State);

                await this.PushState(FailureDetectorStates.WaitForCancelResponse);

                await this.Timer.CancelTimer();
            }
        }

        private async Task TimeoutAction()
        {
            // One attempt is done for this round.
            this.Attempts++;

            // Each round has a maximum number of 2 attempts.
            if (this.Responses.Count < this.Alive.Count && this.Attempts < 2)
            {
                this.Logger.WriteLine("FailureDetector reached maximum number of attempts");
                // Retry by looping back to same state.
                await this.Goto(FailureDetectorStates.SendPing);
            }
            else
            {
                List<Node> failed = new List<Node>();
                lock (this.Alive)
                {
                    foreach (var node in this.Nodes)
                    {
                        if (this.Alive.Contains(node) && !this.Responses.Contains(node))
                        {
                            this.Logger.WriteLine("FailureDetector found a dead node id {0}", node.Id);

                            this.Alive.Remove(node);
                            failed.Add(node);
                        }
                    }
                }

                foreach (var node in failed)
                {
                    // Send failure notification to any clients.
                    foreach (var client in this.Clients)
                    {
                        await client.OnNodeFailed(node);
                    }
                }

                await this.Goto(FailureDetectorStates.Reset);
            }
        }

        private async Task CancelSuccessAction()
        {
            await this.Goto(FailureDetectorStates.Reset);
        }

        private async Task CancelFailure()
        {
            await this.PopState();
        }

        /// <summary>
        /// Prepares the failure detector for the next round.
        /// </summary>
        private async Task ResetOnEntry()
        {
            this.Attempts = 0;
            this.Responses.Clear();

            // Starts the timer with a given timeout value (see details above).
            _ = this.Timer.StartTimer(1000);

            this.PongDeferred = null;  // ignore any pending Pong.

            if (this.TimeoutDeferred)
            {
                await this.Goto(FailureDetectorStates.SendPing);
            }
        }

        public async Task OnTimeout()
        {
            this.Logger.WriteLine("FailureDetector received Timeout from Timer while in State {0}", this.State);
            await Task.Yield();

            switch (this.State)
            {
                case FailureDetectorStates.Init:
                    // error.
                    break;
                case FailureDetectorStates.SendPing:
                    await this.TimeoutAction();
                    break;
                case FailureDetectorStates.WaitForCancelResponse:
                    this.TimeoutDeferred = true;
                    break;
                case FailureDetectorStates.Reset:
                    await this.Goto(FailureDetectorStates.SendPing);
                    break;
                default:
                    break;
            }
        }

        public async Task OnCancelSuccess()
        {
            this.Logger.WriteLine("FailureDetector received CancelSuccess from Timer while in State {0}", this.State);
            await Task.Yield();
            if (this.State == FailureDetectorStates.WaitForCancelResponse)
            {
                await this.CancelSuccessAction();
            }
        }

        public async Task OnCancelFailure()
        {
            this.Logger.WriteLine("FailureDetector received CancelFailure from Timer while in State {0}", this.State);
            await Task.Yield();
            if (this.State == FailureDetectorStates.WaitForCancelResponse)
            {
                await this.CancelFailure();
            }
        }

        public async Task OnPong(Node sender)
        {
            this.Logger.WriteLine("FailureDetector received Pong from {0} while in State {1}", sender.Id, this.State);
            await Task.Yield();
            switch (this.State)
            {
                case FailureDetectorStates.Init:
                    // error.
                    break;
                case FailureDetectorStates.SendPing:
                    await this.PongAction(sender);
                    break;
                case FailureDetectorStates.WaitForCancelResponse:
                    this.PongDeferred = sender;
                    break;
                case FailureDetectorStates.Reset:
                    break;
                default:
                    break;
            }
        }
    }
}
