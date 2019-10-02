// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Coyote;
using Microsoft.Coyote.Machines;

namespace Coyote.Examples.MultiPaxos
{
    internal class Client : Machine
    {
        internal class Config : Event
        {
            public List<MachineId> Servers;

            public Config(List<MachineId> servers)
            {
                this.Servers = servers;
            }
        }

        private List<MachineId> Servers;

        [Start]
        [OnEventGotoState(typeof(Local), typeof(PumpRequestOne))]
        [OnEventDoAction(typeof(Client.Config), nameof(Configure))]
        private class Init : MachineState { }

        private void Configure()
        {
            this.Servers = (this.ReceivedEvent as Config).Servers;
            this.Raise(new Local());
        }

        [OnEntry(nameof(PumpRequestOneOnEntry))]
        [OnEventGotoState(typeof(Response), typeof(PumpRequestTwo))]
        private class PumpRequestOne : MachineState { }

        private void PumpRequestOneOnEntry()
        {
            this.Monitor<ValidityCheck>(new ValidityCheck.MonitorClientSent(1));

            if (this.Random())
            {
                this.Send(this.Servers[0], new PaxosNode.Update(0, 1));
            }
            else
            {
                this.Send(this.Servers[this.Servers.Count - 1], new PaxosNode.Update(0, 1));
            }

            this.Raise(new Response());
        }

        [OnEntry(nameof(PumpRequestTwoOnEntry))]
        [OnEventGotoState(typeof(Response), typeof(Done))]
        private class PumpRequestTwo : MachineState { }

        private void PumpRequestTwoOnEntry()
        {
            this.Monitor<ValidityCheck>(new ValidityCheck.MonitorClientSent(2));

            if (this.Random())
            {
                this.Send(this.Servers[0], new PaxosNode.Update(0, 2));
            }
            else
            {
                this.Send(this.Servers[this.Servers.Count - 1], new PaxosNode.Update(0, 2));
            }

            this.Raise(new Response());
        }

        [OnEntry(nameof(DoneOnEntry))]
        private class Done : MachineState { }

        private void DoneOnEntry()
        {
            this.Raise(new Halt());
        }
    }
}
