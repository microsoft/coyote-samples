// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.Coyote;
using Microsoft.Coyote.Actors;

namespace Coyote.Examples.FailureDetector
{
    /// <summary>
    /// Implementation of a simple node.
    ///
    /// The node responds with a 'Pong' event whenever it receives a 'Ping' event. This is modelling
    /// a commong type of heartbeat that indicates the node is still alive.
    /// </summary>
    internal class Node : StateMachine
    {
        internal class Ping : Event
        {
            public ActorId Client;

            public Ping(ActorId client)
            {
                this.Client = client;
            }
        }

        internal class Pong : Event
        {
            public ActorId Node;

            public Pong(ActorId node)
            {
                this.Node = node;
            }
        }

        [Start]
        [OnEventDoAction(typeof(Ping), nameof(SendPong))]
        private class WaitPing : State { }

        private void SendPong(Event e)
        {
            var client = (e as Ping).Client;
            this.Monitor<Safety>(new Safety.Pong(this.Id));
            this.SendEvent(client, new Pong(this.Id));
        }
    }
}
