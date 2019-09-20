// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.Coyote.IO;
using Microsoft.Coyote.Threading.Tasks;

namespace Coyote.Examples.PingPong
{
    /// <summary>
    /// A Coyote async state machine that models a simple server that sends back a "Pong" to a
    /// given client each time that client sends a "Ping".
    /// </summary>
    internal class Server
    {
        private readonly ILogger Logger;

        public Server(ILogger logger)
        {
            this.Logger = logger;
        }

        public async ControlledTask Ping(Client client)
        {
            await ControlledTask.Yield(); // let's force more concurrency just for fun.
            this.Logger.WriteLine("Server received a ping, sending back a pong.");
            // Handle a ping from the client by sending back an asynchronous Pong event.
            await ControlledTask.Run(client.Pong);
        }
    }
}
