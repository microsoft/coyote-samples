// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.Coyote;
using Microsoft.Coyote.Actors;

namespace Coyote.Examples.CacheCoherence
{
    internal class Client : StateMachine
    {
        internal class Config : Event
        {
            public ActorId Host;
            public bool Pending;

            public Config(ActorId host, bool pending)
                : base()
            {
                this.Host = host;
                this.Pending = pending;
            }
        }

        internal class ReqShare : Event
        {
            public ActorId Client;

            public ReqShare(ActorId client)
                : base()
            {
                this.Client = client;
            }
        }

        internal class ReqExcl : Event
        {
            public ActorId Client;

            public ReqExcl(ActorId client)
                : base()
            {
                this.Client = client;
            }
        }

        internal class InvalidateAck : Event { }

        private class Wait : Event { }

        private class Normal : Event { }

        private class Unit : Event { }

        private ActorId Host;
        private bool Pending;

        [Start]
        [OnEventDoAction(typeof(Config), nameof(Configure))]
        [OnEventGotoState(typeof(Unit), typeof(Invalid))]
        internal class Init : State { }

        internal Transition Configure(Event e)
        {
            this.Host = (e as Config).Host;
            this.Pending = (e as Config).Pending;
            return this.RaiseEvent(new Unit());
        }

        [OnEventGotoState(typeof(CPU.AskShare), typeof(AskedShare))]
        [OnEventGotoState(typeof(CPU.AskExcl), typeof(AskedExcl))]
        [OnEventGotoState(typeof(Host.Invalidate), typeof(Invalidating))]
        [OnEventGotoState(typeof(Host.GrantExcl), typeof(Exclusive))]
        [OnEventGotoState(typeof(Host.GrantShare), typeof(Sharing))]
        internal class Invalid : State { }

        [OnEntry(nameof(AskedShareOnEntry))]
        [OnEventGotoState(typeof(Unit), typeof(InvalidWait))]
        internal class AskedShare : State { }

        internal Transition AskedShareOnEntry()
        {
            this.SendEvent(this.Host, new ReqShare(this.Id));
            this.Pending = true;
            return this.RaiseEvent(new Unit());
        }

        [OnEntry(nameof(AskedExclOnEntry))]
        [OnEventGotoState(typeof(Unit), typeof(InvalidWait))]
        internal class AskedExcl : State { }

        internal Transition AskedExclOnEntry()
        {
            this.SendEvent(this.Host, new ReqExcl(this.Id));
            this.Pending = true;
            return this.RaiseEvent(new Unit());
        }

        [OnEventGotoState(typeof(Host.Invalidate), typeof(Invalidating))]
        [OnEventGotoState(typeof(Host.GrantExcl), typeof(Exclusive))]
        [OnEventGotoState(typeof(Host.GrantShare), typeof(Sharing))]
        [DeferEvents(typeof(CPU.AskShare), typeof(CPU.AskExcl))]
        internal class InvalidWait : State { }

        [OnEntry(nameof(ExcludingOnEntry))]
        [OnEventGotoState(typeof(Unit), typeof(SharingWait))]
        internal class Excluding : State { }

        internal Transition ExcludingOnEntry()
        {
            this.SendEvent(this.Host, new ReqExcl(this.Id));
            this.Pending = true;
            return this.RaiseEvent(new Unit());
        }

        [OnEntry(nameof(SharingOnEntry))]
        [OnEventGotoState(typeof(CPU.AskShare), typeof(Sharing))]
        [OnEventGotoState(typeof(CPU.AskExcl), typeof(Excluding))]
        [OnEventGotoState(typeof(Host.Invalidate), typeof(Invalidating))]
        [OnEventGotoState(typeof(Host.GrantExcl), typeof(Exclusive))]
        [OnEventGotoState(typeof(Host.GrantShare), typeof(Sharing))]
        internal class Sharing : State { }

        internal void SharingOnEntry()
        {
            this.Pending = false;
        }

        [OnEventGotoState(typeof(Host.Invalidate), typeof(Invalidating))]
        [OnEventGotoState(typeof(Host.GrantExcl), typeof(Exclusive))]
        [OnEventGotoState(typeof(Host.GrantShare), typeof(SharingWait))]
        [DeferEvents(typeof(CPU.AskShare), typeof(CPU.AskExcl))]
        internal class SharingWait : State { }

        [OnEntry(nameof(ExclusiveOnEntry))]
        [OnEventGotoState(typeof(Host.Invalidate), typeof(Invalidating))]
        [OnEventGotoState(typeof(Host.GrantExcl), typeof(Exclusive))]
        [OnEventGotoState(typeof(Host.GrantShare), typeof(Sharing))]
        [IgnoreEvents(typeof(CPU.AskShare), typeof(CPU.AskExcl))]
        internal class Exclusive : State { }

        internal void ExclusiveOnEntry()
        {
            this.Pending = false;
        }

        [OnEntry(nameof(InvalidatingOnEntry))]
        [OnEventGotoState(typeof(Wait), typeof(InvalidWait))]
        [OnEventGotoState(typeof(Normal), typeof(Invalid))]
        internal class Invalidating : State { }

        internal Transition InvalidatingOnEntry()
        {
            this.SendEvent(this.Host, new InvalidateAck());

            if (this.Pending)
            {
                return this.RaiseEvent(new Wait());
            }

            return this.RaiseEvent(new Normal());
        }
    }
}
