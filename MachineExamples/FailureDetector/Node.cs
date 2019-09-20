// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.Coyote.Machines;

namespace Coyote.Examples.FailureDetector
{
    /// <summary>
    /// Implementation of a simple node.
    ///
    /// The node responds with a 'Pong' event whenever it receives a 'Ping' event. This is modelling
    /// a commong type of heartbeat that indicates the node is still alive.
    /// </summary>
    internal class Node : Machine
    {
        internal class Ping : Event
        {
            public MachineId Client;

            public Ping(MachineId client)
            {
                this.Client = client;
            }
        }

        internal class Pong : Event
        {
            public MachineId Node;

            public Pong(MachineId node)
            {
                this.Node = node;
            }
        }

        [Start]
        [OnEventDoAction(typeof(Ping), nameof(SendPong))]
        private class WaitPing : MachineState { }

        private void SendPong()
        {
            var client = (this.ReceivedEvent as Ping).Client;
            this.Monitor<Safety>(new Safety.Pong(this.Id));
            this.Send(client, new Pong(this.Id));
        }
    }
}
