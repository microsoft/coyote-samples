// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Coyote;
using Microsoft.Coyote.Actors;

namespace Coyote.Examples.MultiPaxos
{
    internal class Client : StateMachine
    {
        internal class Config : Event
        {
            public List<ActorId> Servers;

            public Config(List<ActorId> servers)
            {
                this.Servers = servers;
            }
        }

        private List<ActorId> Servers;

        [Start]
        [OnEventGotoState(typeof(Local), typeof(PumpRequestOne))]
        [OnEventDoAction(typeof(Config), nameof(Configure))]
        private class Init : State { }

        private Transition Configure(Event e)
        {
            this.Servers = (e as Config).Servers;
            return this.RaiseEvent(new Local());
        }

        [OnEntry(nameof(PumpRequestOneOnEntry))]
        [OnEventGotoState(typeof(Response), typeof(PumpRequestTwo))]
        private class PumpRequestOne : State { }

        private Transition PumpRequestOneOnEntry()
        {
            this.Monitor<ValidityCheck>(new ValidityCheck.MonitorClientSent(1));

            if (this.Random())
            {
                this.SendEvent(this.Servers[0], new PaxosNode.Update(0, 1));
            }
            else
            {
                this.SendEvent(this.Servers[this.Servers.Count - 1], new PaxosNode.Update(0, 1));
            }

            return this.RaiseEvent(new Response());
        }

        [OnEntry(nameof(PumpRequestTwoOnEntry))]
        [OnEventGotoState(typeof(Response), typeof(Done))]
        private class PumpRequestTwo : State { }

        private Transition PumpRequestTwoOnEntry()
        {
            this.Monitor<ValidityCheck>(new ValidityCheck.MonitorClientSent(2));

            if (this.Random())
            {
                this.SendEvent(this.Servers[0], new PaxosNode.Update(0, 2));
            }
            else
            {
                this.SendEvent(this.Servers[this.Servers.Count - 1], new PaxosNode.Update(0, 2));
            }

            return this.RaiseEvent(new Response());
        }

        [OnEntry(nameof(DoneOnEntry))]
        private class Done : State { }

        private Transition DoneOnEntry()
        {
            return this.Halt();
        }
    }
}
