// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.Coyote;
using Microsoft.Coyote.Actors;

namespace Coyote.Examples.PingPong.AsyncAwait
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

        /// <summary>
        /// Reference to the server machine.
        /// </summary>
        private ActorId Server;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        private class Init : State { }

        private Transition InitOnEntry(Event e)
        {
            // Receives a reference to a server machine (as a payload of
            // the 'Config' event).
            this.Server = (e as Config).Server;

            // Notifies the Coyote runtime that the machine must transition
            // to the 'Active' state when 'InitOnEntry' returns.
            return this.GotoState<Active>();
        }

        /// <summary>
        /// The active state
        /// </summary>
        [OnEntry(nameof(ActiveOnEntry))]
        private class Active : State { }

        private async Task<Transition> ActiveOnEntry()
        {
            // A counter for ping-pong turns.
            int counter = 0;
            while (counter < 5)
            {
                // Sends (asynchronously) a 'Ping' event to the server that contains
                // a reference to this client as a payload.
                this.SendEvent(this.Server, new Ping(this.Id));

                // Invoking 'Receive' will cause the machine to wait (asynchronously)
                // until a 'Pong' event is received. The event will then get dequeued
                // and execution will resume.
                await this.ReceiveEventAsync(typeof(Server.Pong));

                counter++;

                Console.WriteLine("Client request: {0} / 5", counter);
            }

            // If 5 'Ping' events were sent, then raise the special event 'Halt'.
            //
            // Raising an event, notifies the Coyote runtime to execute the event handler
            // that corresponds to this event in the current state, when 'SendPing'
            // returns.
            //
            // In this case, when the machine handles the special event 'Halt', it
            // will terminate the machine and release any resources. Note that the
            // 'Halt' event is handled automatically, the user does not need to
            // declare an event handler in the state declaration.
            return this.Halt();
        }
    }
}
