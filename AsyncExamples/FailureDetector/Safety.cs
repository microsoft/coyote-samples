// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Coyote;
using Microsoft.Coyote.Machines;

namespace Coyote.Examples.FailureDetector
{
    /// <summary>
    /// Monitors allow the Coyote testing engine to detect global safety property
    /// violations. This monitor gathers 'Ping' and 'Pong' events and manages
    /// the per-client history.
    ///
    /// 'Ping' increments the client ping count and 'Pong' decrements it.
    ///
    /// A safety violation is reported if the ping count is less than 0 or
    /// greater than 3 (these indicate unmatched updates).
    /// </summary>
    internal class Safety : Monitor
    {
        internal class Ping : Event
        {
            public Node Client;

            public Ping(Node client)
            {
                this.Client = client;
            }
        }

        internal class Pong : Event
        {
            public Node Node;

            public Pong(Node node)
            {
                this.Node = node;
            }
        }

        private Dictionary<Node, int> Pending;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventDoAction(typeof(Ping), nameof(PingAction))]
        [OnEventDoAction(typeof(Pong), nameof(PongAction))]
        private class Init : MonitorState { }

        private void InitOnEntry()
        {
            this.Pending = new Dictionary<Node, int>();
        }

        private void PingAction()
        {
            var client = (this.ReceivedEvent as Ping).Client;
            lock (this.Pending)
            {
                if (!this.Pending.ContainsKey(client))
                {
                    this.Pending[client] = 0;
                }

                this.Pending[client] = this.Pending[client] + 1;
            }

            this.Assert(this.Pending[client] <= 3, $"'{client}' ping count must be <= 3.");
        }

        private void PongAction()
        {
            var node = (this.ReceivedEvent as Pong).Node;
            this.Assert(this.Pending.ContainsKey(node), $"'{node}' is not in pending set.");
            this.Assert(this.Pending[node] > 0, $"'{node}' ping count must be > 0.");
            lock (this.Pending)
            {
                this.Pending[node] = this.Pending[node] - 1;
            }
        }
    }
}
