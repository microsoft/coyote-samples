// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.Coyote;
using Microsoft.Coyote.Machines;

namespace Coyote.Examples.CacheCoherence
{
    internal class Client : Machine
    {
        internal class Config : Event
        {
            public MachineId Host;
            public bool Pending;

            public Config(MachineId host, bool pending)
                : base()
            {
                this.Host = host;
                this.Pending = pending;
            }
        }

        internal class ReqShare : Event
        {
            public MachineId Client;

            public ReqShare(MachineId client)
                : base()
            {
                this.Client = client;
            }
        }

        internal class ReqExcl : Event
        {
            public MachineId Client;

            public ReqExcl(MachineId client)
                : base()
            {
                this.Client = client;
            }
        }

        internal class InvalidateAck : Event { }

        private class Wait : Event { }

        private class Normal : Event { }

        private class Unit : Event { }

        private MachineId Host;
        private bool Pending;

        [Start]
        [OnEventDoAction(typeof(Config), nameof(Configure))]
        [OnEventGotoState(typeof(Unit), typeof(Invalid))]
        internal class Init : MachineState { }

        internal void Configure()
        {
            this.Host = (this.ReceivedEvent as Config).Host;
            this.Pending = (this.ReceivedEvent as Config).Pending;
            this.Raise(new Unit());
        }

        [OnEventGotoState(typeof(CPU.AskShare), typeof(AskedShare))]
        [OnEventGotoState(typeof(CPU.AskExcl), typeof(AskedExcl))]
        [OnEventGotoState(typeof(Host.Invalidate), typeof(Invalidating))]
        [OnEventGotoState(typeof(Host.GrantExcl), typeof(Exclusive))]
        [OnEventGotoState(typeof(Host.GrantShare), typeof(Sharing))]
        internal class Invalid : MachineState { }

        [OnEntry(nameof(AskedShareOnEntry))]
        [OnEventGotoState(typeof(Unit), typeof(InvalidWait))]
        internal class AskedShare : MachineState { }

        internal void AskedShareOnEntry()
        {
            this.Send(this.Host, new ReqShare(this.Id));
            this.Pending = true;
            this.Raise(new Unit());
        }

        [OnEntry(nameof(AskedExclOnEntry))]
        [OnEventGotoState(typeof(Unit), typeof(InvalidWait))]
        internal class AskedExcl : MachineState { }

        internal void AskedExclOnEntry()
        {
            this.Send(this.Host, new ReqExcl(this.Id));
            this.Pending = true;
            this.Raise(new Unit());
        }

        [OnEventGotoState(typeof(Host.Invalidate), typeof(Invalidating))]
        [OnEventGotoState(typeof(Host.GrantExcl), typeof(Exclusive))]
        [OnEventGotoState(typeof(Host.GrantShare), typeof(Sharing))]
        [DeferEvents(typeof(CPU.AskShare), typeof(CPU.AskExcl))]
        internal class InvalidWait : MachineState { }

        [OnEntry(nameof(ExcludingOnEntry))]
        [OnEventGotoState(typeof(Unit), typeof(SharingWait))]
        internal class Excluding : MachineState { }

        internal void ExcludingOnEntry()
        {
            this.Send(this.Host, new ReqExcl(this.Id));
            this.Pending = true;
            this.Raise(new Unit());
        }

        [OnEntry(nameof(SharingOnEntry))]
        [OnEventGotoState(typeof(CPU.AskShare), typeof(Sharing))]
        [OnEventGotoState(typeof(CPU.AskExcl), typeof(Excluding))]
        [OnEventGotoState(typeof(Host.Invalidate), typeof(Invalidating))]
        [OnEventGotoState(typeof(Host.GrantExcl), typeof(Exclusive))]
        [OnEventGotoState(typeof(Host.GrantShare), typeof(Sharing))]
        internal class Sharing : MachineState { }

        internal void SharingOnEntry()
        {
            this.Pending = false;
        }

        [OnEventGotoState(typeof(Host.Invalidate), typeof(Invalidating))]
        [OnEventGotoState(typeof(Host.GrantExcl), typeof(Exclusive))]
        [OnEventGotoState(typeof(Host.GrantShare), typeof(SharingWait))]
        [DeferEvents(typeof(CPU.AskShare), typeof(CPU.AskExcl))]
        internal class SharingWait : MachineState { }

        [OnEntry(nameof(ExclusiveOnEntry))]
        [OnEventGotoState(typeof(Host.Invalidate), typeof(Invalidating))]
        [OnEventGotoState(typeof(Host.GrantExcl), typeof(Exclusive))]
        [OnEventGotoState(typeof(Host.GrantShare), typeof(Sharing))]
        [IgnoreEvents(typeof(CPU.AskShare), typeof(CPU.AskExcl))]
        internal class Exclusive : MachineState { }

        internal void ExclusiveOnEntry()
        {
            this.Pending = false;
        }

        [OnEntry(nameof(InvalidatingOnEntry))]
        [OnEventGotoState(typeof(Wait), typeof(InvalidWait))]
        [OnEventGotoState(typeof(Normal), typeof(Invalid))]
        internal class Invalidating : MachineState { }

        internal void InvalidatingOnEntry()
        {
            this.Send(this.Host, new InvalidateAck());

            if (this.Pending)
            {
                this.Raise(new Wait());
            }
            else
            {
                this.Raise(new Normal());
            }
        }
    }
}
