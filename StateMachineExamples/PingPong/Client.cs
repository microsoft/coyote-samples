// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.Coyote;
using Microsoft.Coyote.Actors;

namespace Coyote.Examples.PingPong
{
    /// <summary>
    /// A Coyote machine that models a simple client.
    ///
    /// It sends 'Ping' events to a server, and handles received 'Pong' event.
    /// </summary>
    internal class Client : StateMachine
    {
        /// <summary>
        /// Event declaration of a 'Config' event that contains payload.
        /// </summary>
        internal class Config : Event
        {
            /// <summary>
            /// The payload of the event. It is a reference to the server machine
            /// (send by the 'NetworkEnvironment' machine upon creation of the client).
            /// </summary>
            public ActorId Server;

            public Config(ActorId server)
            {
                this.Server = server;
            }
        }

        /// <summary>
        /// Event declaration of a 'Unit' event that does not contain any payload.
        /// </summary>
        internal class Unit : Event { }

        /// <summary>
        /// Event declaration of a 'Ping' event that contains payload.
        /// </summary>
        internal class Ping : Event
        {
            /// <summary>
            /// The payload of the event. It is a reference to the client machine.
            /// </summary>
            public ActorId Client;

            public Ping(ActorId client)
            {
                this.Client = client;
            }
        }

        internal class TerminateEvent : Event
        {
        }

        /// <summary>
        /// Reference to the server machine.
        /// </summary>
        private ActorId Server;

        /// <summary>
        /// A counter for ping-pong turns.
        /// </summary>
        private int Counter;

        /// <summary>
        /// Init is a state of your machine, notice it inherits from MachineState.
        /// States can have associated custom attributes that define how this state
        /// can behave.  In this case we are saying it is the "Start" state of the
        /// Client and when the client enters this state call the "InitOnEntry" method.
        /// </summary>
        [Start]
        [OnEntry(nameof(InitOnEntry))]
        private class Init : State { }

        /// <summary>
        /// Called when the Client enters the "Init" state.
        /// </summary>
        private void InitOnEntry()
        {
            // Receives a reference to a server machine (as a payload of
            // the 'Config' event).
            this.Server = (this.ReceivedEvent as Config).Server;
            this.Counter = 0;

            // Notifies the Coyote runtime that the machine must transition
            // to the 'Active' state when 'InitOnEntry' returns.
            this.Goto<Active>();
        }

        /// <summary>
        /// The next state on our machine is called the "Active" state.  Like the Init
        /// state it also has an "OnEntry" attribute.  It also has an "OnEventDoAction"
        /// attribute that will execute (asynchrously) the 'SendPing' method, whenever
        /// a 'Pong' event is dequeued while the client machine is in the 'Active' state.
        /// Notice how this can now help you build up some pretty complex state machinery
        /// with very little code.
        /// </summary>
        [OnEntry(nameof(ActiveOnEntry))]
        [OnEventDoAction(typeof(Server.Pong), nameof(SendPing))]
        [OnEventGotoState(typeof(TerminateEvent), typeof(Terminating))]
        private class Active : State { }

        /// <summary>
        /// Called when the Client enters the Active state.  To kick off the Ping/Pong
        /// events, this method starts the process by sending the first Ping.
        /// </summary>
        private void ActiveOnEntry()
        {
            this.SendPing();
        }

        /// <summary>
        /// This method is called from two places, first from ActiveOnEntry and also
        /// when the Server sends a Pong event.
        /// </summary>
        private void SendPing()
        {
            this.Counter++;

            // Sends (asynchronously) a 'Ping' event to the server that contains
            // a reference to this client as a payload.
            this.SendEvent(this.Server, new Ping(this.Id));

            this.Logger.WriteLine("Client request: {0} / 5", this.Counter);

            if (this.Counter == 5)
            {
                // If 5 'Ping' events where sent, then go to the Terminating state
                // otherwise the Ping/Pong will just go on forever!
                this.Logger.WriteLine("Client terminating");

                // Notice that you can send an event to yourself using the "Raise" method.
                // Raising an event, notifies the Coyote runtime to execute the event handler
                // that corresponds to this event in the current state, when the calling method
                // returns.  In this case the event handler is the "OnEventGotoState" attribute
                // which will transition us to the Terminating state.  This is equivalent to
                // using this.Goto<Terminating>().
                this.RaiseEvent(new TerminateEvent());
            }
        }

        /// <summary>
        /// The "Terminating" state cannot receive a Server.Pong event.
        /// and is only here to halt the state machine.
        /// </summary>
        [OnEntry(nameof(TerminatingOnEntry))]
        private class Terminating : State { }

        private void TerminatingOnEntry()
        {
            this.Logger.WriteLine("Client halting");

            // In this case, when the machine handles the special event 'Halt', it
            // will terminate the machine and release any resources. Note that the
            // 'Halt' event is handled automatically, the user does not need to
            // declare an event handler in the state declarations.
            this.RaiseEvent(new HaltEvent());
        }
    }
}
