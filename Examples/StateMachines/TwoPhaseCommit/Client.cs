// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.Coyote;
using Microsoft.Coyote.Actors;

namespace Coyote.Examples.TwoPhaseCommit
{
    internal class Client : StateMachine
    {
        internal class Config : Event
        {
            public ActorId Coordinator;

            public Config(ActorId coordinator)
                : base()
            {
                this.Coordinator = coordinator;
            }
        }

        internal class WriteReq : Event
        {
            public PendingWriteRequest PendingWriteReq;

            public WriteReq(PendingWriteRequest req)
                : base()
            {
                this.PendingWriteReq = req;
            }
        }

        internal class ReadReq : Event
        {
            public ActorId Client;
            public int Idx;

            public ReadReq(ActorId client, int idx)
                : base()
            {
                this.Client = client;
                this.Idx = idx;
            }
        }

        private class Unit : Event { }

        private ActorId Coordinator;
        private int Idx;
        private int Val;

        [Start]
        [OnEventGotoState(typeof(Unit), typeof(DoWrite))]
        [OnEventDoAction(typeof(Config), nameof(Configure))]
        private class Init : State { }

        private Transition Configure(Event e)
        {
            this.Coordinator = (e as Config).Coordinator;
            return this.RaiseEvent(new Unit());
        }

        [OnEntry(nameof(DoWriteOnEntry))]
        [OnEventGotoState(typeof(Coordinator.WriteSuccess), typeof(DoRead))]
        [OnEventGotoState(typeof(Coordinator.WriteFail), typeof(End))]
        private class DoWrite : State { }

        private void DoWriteOnEntry()
        {
            this.Idx = this.ChooseIndex();
            this.Val = this.ChooseValue();

            this.SendEvent(this.Coordinator, new WriteReq(new PendingWriteRequest(
                this.Id, this.Idx, this.Val)));
        }

        private int ChooseIndex()
        {
            if (this.Random())
            {
                return 0;
            }
            else
            {
                return 1;
            }
        }

        private int ChooseValue()
        {
            if (this.Random())
            {
                return 0;
            }
            else
            {
                return 1;
            }
        }

        [OnEntry(nameof(DoReadOnEntry))]
        [OnEventGotoState(typeof(Coordinator.ReadSuccess), typeof(End))]
        [OnEventGotoState(typeof(Coordinator.ReadFail), typeof(End))]
        private class DoRead : State { }

        private void DoReadOnEntry()
        {
            this.SendEvent(this.Coordinator, new ReadReq(this.Id, this.Idx));
        }

        private class End : State { }
    }
}
