// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.Coyote;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Specifications;
using Task = Microsoft.Coyote.Threading.Tasks.ControlledTask;

namespace Coyote.Examples.FailureDetector
{
    /// <summary>
    /// The interface used to get async call back from the Node
    /// </summary>
    internal interface INodeClient
    {
        Task OnPong(Node sender);
    }

    /// <summary>
    /// Implementation of a simple node.
    ///
    /// The node responds with a 'Pong' event whenever it receives a 'Ping' event. This is modelling
    /// a commong type of heartbeat that indicates the node is still alive.
    /// </summary>
    internal class Node
    {
        internal readonly int Id;

        private readonly ILogger Logger;

        public Node(int id, ILogger logger)
        {
            this.Id = id;
            this.Logger = logger;
        }

        internal async Task Ping(INodeClient client)
        {
            if (!this.Halted)
            {
                this.Logger.WriteLine("Node {0} received Ping", this.Id);
                await Task.Delay(1);
                Specification.Monitor<Safety>(new Safety.Pong(this));
                await client.OnPong(this);
            }
            else
            {
                this.Logger.WriteLine("Node {0} ignoreing Ping since it is in the Halted state", this.Id);
            }
        }

        private bool Halted;

        internal void Halt()
        {
            this.Halted = true;
        }
    }
}
