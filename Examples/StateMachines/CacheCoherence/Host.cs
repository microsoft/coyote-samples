// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Coyote;
using Microsoft.Coyote.Actors;

namespace Coyote.Examples.CacheCoherence
{
    internal class Host : StateMachine
    {
        internal class GrantExcl : Event { }

        internal class GrantShare : Event { }

        internal class Invalidate : Event { }

        private class GrantAccess : Event { }

        private class NeedInvalidate : Event { }

        private class Unit : Event { }

        private ActorId CurrentClient;
        private Tuple<ActorId, ActorId, ActorId> Clients;
        private ActorId CurrentCPU;
        private List<ActorId> SharerList;

        private bool IsCurrentReqExcl;
        private bool IsExclGranted;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(Unit), typeof(Receiving))]
        internal class Init : State { }

        internal Transition InitOnEntry()
        {
            this.SharerList = new List<ActorId>();

            this.Clients = Tuple.Create(
                this.CreateActor(typeof(Client)),
                this.CreateActor(typeof(Client)),
                this.CreateActor(typeof(Client)));

            this.SendEvent(this.Clients.Item1, new Client.Config(this.Id, false));
            this.SendEvent(this.Clients.Item2, new Client.Config(this.Id, false));
            this.SendEvent(this.Clients.Item3, new Client.Config(this.Id, false));

            this.CurrentClient = null;
            this.CurrentCPU = this.CreateActor(typeof(CPU));
            this.SendEvent(this.CurrentCPU, new CPU.Config(this.Clients));

            return this.RaiseEvent(new Unit());
        }

        [OnEventGotoState(typeof(Client.ReqShare), typeof(ShareRequest))]
        [OnEventGotoState(typeof(Client.ReqExcl), typeof(ExclRequest))]
        [DeferEvents(typeof(Client.InvalidateAck))]
        internal class Receiving : State { }

        [OnEntry(nameof(ShareRequestOnEntry))]
        [OnEventGotoState(typeof(Unit), typeof(ProcessingRequest))]
        internal class ShareRequest : State { }

        internal Transition ShareRequestOnEntry(Event e)
        {
            this.CurrentClient = (e as Client.ReqShare).Client;
            this.IsCurrentReqExcl = false;
            return this.RaiseEvent(new Unit());
        }

        [OnEntry(nameof(ExclRequestOnEntry))]
        [OnEventGotoState(typeof(Unit), typeof(ProcessingRequest))]
        internal class ExclRequest : State { }

        internal Transition ExclRequestOnEntry(Event e)
        {
            this.CurrentClient = (e as Client.ReqExcl).Client;
            this.IsCurrentReqExcl = true;
            return this.RaiseEvent(new Unit());
        }

        [OnEntry(nameof(ProcessRequestOnEntry))]
        [OnEventGotoState(typeof(NeedInvalidate), typeof(Invalidating))]
        [OnEventGotoState(typeof(GrantAccess), typeof(GrantingAccess))]
        internal class ProcessingRequest : State { }

        internal Transition ProcessRequestOnEntry()
        {
            if (this.IsCurrentReqExcl || this.IsExclGranted)
            {
                return this.RaiseEvent(new NeedInvalidate());
            }

            return this.RaiseEvent(new GrantAccess());
        }

        [OnEntry(nameof(InvalidatingOnEntry))]
        [OnEventGotoState(typeof(GrantAccess), typeof(GrantingAccess))]
        [OnEventDoAction(typeof(Client.InvalidateAck), nameof(RecAck))]
        [DeferEvents(typeof(Client.ReqShare), typeof(Client.ReqExcl))]
        internal class Invalidating : State { }

        internal Transition InvalidatingOnEntry()
        {
            if (this.SharerList.Count == 0)
            {
                return this.RaiseEvent(new GrantAccess());
            }

            foreach (var m in this.SharerList)
            {
                this.SendEvent(m, new Invalidate());
            }

            return default;
        }

        internal Transition RecAck()
        {
            this.SharerList.RemoveAt(0);
            if (this.SharerList.Count == 0)
            {
                return this.RaiseEvent(new GrantAccess());
            }

            return default;
        }

        [OnEntry(nameof(GrantingAccessOnEntry))]
        [OnEventGotoState(typeof(Unit), typeof(Receiving))]
        internal class GrantingAccess : State { }

        internal Transition GrantingAccessOnEntry()
        {
            if (this.IsCurrentReqExcl)
            {
                this.IsExclGranted = true;
                this.SendEvent(this.CurrentClient, new GrantExcl());
            }
            else
            {
                this.SendEvent(this.CurrentClient, new GrantShare());
            }

            this.SharerList.Insert(0, this.CurrentClient);
            return this.RaiseEvent(new Unit());
        }
    }
}
