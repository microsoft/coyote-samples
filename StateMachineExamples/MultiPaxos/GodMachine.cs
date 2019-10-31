// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Coyote.Actors;

namespace Coyote.Examples.MultiPaxos
{
    internal class GodMachine : StateMachine
    {
        private List<ActorId> PaxosNodes;
        private ActorId Client;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        private class Init : State { }

        private void InitOnEntry()
        {
            this.PaxosNodes = new List<ActorId>();

            this.PaxosNodes.Insert(0, this.CreateStateMachine(typeof(PaxosNode)));
            this.SendEvent(this.PaxosNodes[0], new PaxosNode.Config(3));

            this.PaxosNodes.Insert(0, this.CreateStateMachine(typeof(PaxosNode)));
            this.SendEvent(this.PaxosNodes[0], new PaxosNode.Config(2));

            this.PaxosNodes.Insert(0, this.CreateStateMachine(typeof(PaxosNode)));
            this.SendEvent(this.PaxosNodes[0], new PaxosNode.Config(1));

            foreach (var node in this.PaxosNodes)
            {
                this.SendEvent(node, new PaxosNode.AllNodes(this.PaxosNodes));
            }

            this.Client = this.CreateStateMachine(typeof(Client));
            this.SendEvent(this.Client, new Client.Config(this.PaxosNodes));
        }
    }
}
