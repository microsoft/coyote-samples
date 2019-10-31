// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Coyote;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Threading.Tasks;

namespace Coyote.Examples.PingPong
{
    /// <summary>
    /// A Coyote async state machine that models a simple client that will "Ping" a Server and
    /// every time the Server sends back a "Pong" it sends another "Ping".  Hence the name PingPong.
    /// </summary>
    internal class Client
    {
        /// <summary>
        /// Reference to the server machine.
        /// </summary>
        private readonly Server Server;

        /// <summary>
        /// A counter for ping-pong turns.
        /// </summary>
        private int Counter;

        /// <summary>
        /// A simple logger object
        /// </summary>
        private readonly ILogger Logger;

        /// <summary>
        /// An indication of the state of the client.
        /// </summary>
        private bool IsActive;

        public Client(Server server, ILogger logger)
        {
            this.Server = server;

            this.Logger = logger;
        }

        /// <summary>
        /// When the Client is activated it starts the Ping/Pong process
        /// by sending the first Ping to the server.
        /// </summary>
        /// <returns>Returns a Coyote ControlledTask</returns>
        public async ControlledTask Activate()
        {
            await ControlledTask.Yield(); // let's force more concurrency just for fun.
            this.IsActive = true;

            // When we are activated, we kick things off by sending a ping to the server.
            this.Logger.WriteLine("Client is activated");
            await this.SendPing();
        }

        /// <summary>
        /// Send an async "Ping" "event" to the Server.  The term "event" is used here even though
        /// it is just an async method call, because this is the terminology used in Coyote State
        /// Machine Model.  These async events will be fully explored by the CoyoteTester, testing
        /// all kinds of timings and interleavings of async events in order to find bugs in your code.
        /// This is made possible by the ControlledTask object that is used to wrap the
        /// System.Threading.Tasks.Task object.
        /// </summary>
        /// <returns>Returns a Coyote ControlledTask</returns>
        public async ControlledTask SendPing()
        {
            await ControlledTask.Yield(); // let's force more concurrency just for fun.
            this.Logger.WriteLine("Client sending Ping to Server");

            // Sends (asynchronously) a 'Ping' event to the server that contains
            // a reference to this client as a payload.

            this.Counter++;
            this.Logger.WriteLine("Client request: {0} / 5", this.Counter);

            if (this.Counter == 5)
            {
                // So we don't PingPong forever, this little counter will tell the Client to stop
                // after 5 pings.  Stopping is easy, we simply stop by not sending another ping.
                this.Logger.WriteLine("Client halting");
                this.IsActive = false;
            }
            else
            {
                await this.Server.Ping(this);
            }
        }

        /// <summary>
        /// This is the async method that handles the "Pong" event received from the server.
        /// You might think server.Ping calling client.Pong over and over might result in a stack
        /// overflow, but this is not the case for "async" methods are actually run asynchronously.
        /// The C# compiler actually sets up an IAsyncStateMachine to manage all this under the
        /// covers.  If you put a breakpoint on this method you will see the callstack is always
        /// coming from ThreadPoolWorkQueue.Dispatch.
        /// </summary>
        public async ControlledTask Pong()
        {
            await ControlledTask.Yield(); // let's force more concurrency just for fun.
            this.Logger.WriteLine("Client received a pong.");

            // This is where Coyote can really get interesting.  You can write what are called
            // "Specifications" which are checked by the CoyoteTester to ensure certain invariants
            // remain true during the test process.  In this case we want to ensure a Pong never
            // arrives here unless we are in the "active state" (in other words, we sent a Ping
            // and we are not in the stopped state).
            Specification.Assert(this.IsActive, "Client is not active");

            // Send another ping.
            await this.SendPing();
        }
    }
}
