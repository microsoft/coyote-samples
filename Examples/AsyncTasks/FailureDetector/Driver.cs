// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Specifications;
using Task = Microsoft.Coyote.Threading.Tasks.ControlledTask;

namespace Coyote.Examples.FailureDetector
{
    /// <summary>
    /// This is the test harness responsible for creating a user-defined number of nodes, and
    /// registering them with a failure detector machine.
    ///
    /// The driver is also responsible for injecting failures to the nodes for testing purposes.
    /// </summary>
    internal class Driver : IFailureDetectorClient
    {
        private readonly ILogger Logger;

        public Driver(int numberOfNodes, ILogger logger)
        {
            this.NumOfNodes = numberOfNodes;
            this.Logger = logger;

            // Initializes the nodes.
            this.Nodes = new HashSet<Node>();
            for (int i = 0; i < this.NumOfNodes; i++)
            {
                var node = new Node(i, logger);
                this.Nodes.Add(node);
            }
        }

        private FailureDetector FailureDetector;
        private readonly HashSet<Node> Nodes;
        private readonly int NumOfNodes;

        public async Task Run()
        {
            // Notifies the liveness monitor that the nodes are initialized.
            Specification.Monitor<Liveness>(new Liveness.RegisterNodes(this.Nodes));

            this.FailureDetector = new FailureDetector(this.Nodes, this.Logger);
            this.FailureDetector.RegisterClient(this);
            await this.FailureDetector.Init();

            await this.InjectFailures();
        }

        /// <summary>
        /// Injects failures (modelled with the special Coyote event 'halt').
        /// </summary>
        private async Task InjectFailures()
        {
            await Task.Delay(10);
            foreach (var node in this.Nodes)
            {
                node.Halt();
            }
        }

        /// <summary>
        /// Notify liveness monitor of node failure.
        /// </summary>
        public async Task OnNodeFailed(Node node)
        {
            await Task.Yield();
            Specification.Monitor<Liveness>(new Liveness.NodeFailed(node));
        }
    }
}
