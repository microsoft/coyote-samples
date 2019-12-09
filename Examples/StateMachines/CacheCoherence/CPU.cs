// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.Coyote;
using Microsoft.Coyote.Actors;

namespace Coyote.Examples.CacheCoherence
{
    internal class CPU : StateMachine
    {
        internal class Config : Event
        {
            public Tuple<ActorId, ActorId, ActorId> Clients;

            public Config(Tuple<ActorId, ActorId, ActorId> clients)
                : base()
            {
                this.Clients = clients;
            }
        }

        internal class AskShare : Event { }

        internal class AskExcl : Event { }

        private class Unit : Event { }

        private Tuple<ActorId, ActorId, ActorId> Cache;

        [Start]
        [OnEventDoAction(typeof(Config), nameof(Configure))]
        [OnEventGotoState(typeof(Unit), typeof(MakingReq))]
        internal class Init : State { }

        internal Transition Configure(Event e)
        {
            this.Cache = (e as Config).Clients;
            return this.RaiseEvent(new Unit());
        }

        [OnEntry(nameof(MakingReqOnEntry))]
        [OnEventGotoState(typeof(Unit), typeof(MakingReq))]
        internal class MakingReq : State { }

        internal Transition MakingReqOnEntry()
        {
            if (this.Random())
            {
                if (this.Random())
                {
                    this.SendEvent(this.Cache.Item1, new AskShare());
                }
                else
                {
                    this.SendEvent(this.Cache.Item1, new AskExcl());
                }
            }
            else if (this.Random())
            {
                if (this.Random())
                {
                    this.SendEvent(this.Cache.Item2, new AskShare());
                }
                else
                {
                    this.SendEvent(this.Cache.Item2, new AskExcl());
                }
            }
            else
            {
                if (this.Random())
                {
                    this.SendEvent(this.Cache.Item3, new AskShare());
                }
                else
                {
                    this.SendEvent(this.Cache.Item3, new AskExcl());
                }
            }

            return this.RaiseEvent(new Unit());
        }
    }
}
