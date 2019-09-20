// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.Coyote.Machines;
using Microsoft.Coyote.Threading.Tasks;

namespace Coyote.Examples.PingPong
{
    /// <summary>
    /// This machine acts as a test harness. It models a network environment
    /// by creating a 'Server' and a 'Client' object.
    /// </summary>
    internal class NetworkEnvironment
    {
        private readonly Server Server;
        private readonly Client Client;

        public NetworkEnvironment()
        {
            var logger = new Microsoft.Coyote.IO.ConsoleLogger();

            // Creates the server object.
            this.Server = new Server(logger);

            // Creates a client machine, and passes the Server object
            this.Client = new Client(this.Server, logger);
        }

        /// <summary>
        /// This is the entry point to the async state machine.
        /// Notice that we are using the Coyote ControlledTask instead of
        /// System.Threading.Tasks.Task.  The ControlledTask is key
        /// to getting all the benefits of CoyoteTester.
        /// </summary>
        internal async ControlledTask Run()
        {
            await this.Client.Activate();
        }
    }
}
