// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Coyote;
using Microsoft.Coyote.Actors;

namespace Coyote.Examples.TwoPhaseCommit
{
    internal class Replica : StateMachine
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

        internal class RespReplicaCommit : Event
        {
            public int SeqNum;

            public RespReplicaCommit(int seqNum)
                : base()
            {
                this.SeqNum = seqNum;
            }
        }

        internal class RespReplicaAbort : Event
        {
            public int SeqNum;

            public RespReplicaAbort(int seqNum)
                : base()
            {
                this.SeqNum = seqNum;
            }
        }

        private class Unit : Event { }

        private ActorId Coordinator;
        private Dictionary<int, int> Data;
        private PendingWriteRequest PendingWriteReq;
        private int LastSeqNum;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(Unit), typeof(Loop))]
        [OnEventDoAction(typeof(Config), nameof(Configure))]
        private class Init : State { }

        private void InitOnEntry()
        {
            this.Data = new Dictionary<int, int>();
        }

        private Transition Configure(Event e)
        {
            this.Coordinator = (e as Config).Coordinator;
            this.LastSeqNum = 0;
            return this.RaiseEvent(new Unit());
        }

        [OnEventDoAction(typeof(Coordinator.GlobalAbort), nameof(GlobalAbortAction))]
        [OnEventDoAction(typeof(Coordinator.GlobalCommit), nameof(GlobalCommitAction))]
        [OnEventDoAction(typeof(Coordinator.ReqReplica), nameof(HandleReplica))]
        private class Loop : State { }

        private void GlobalAbortAction(Event e)
        {
            var lastSeqNum = (e as Coordinator.GlobalAbort).SeqNum;
            this.Assert(this.PendingWriteReq.SeqNum >= lastSeqNum);
            if (this.PendingWriteReq.SeqNum == lastSeqNum)
            {
                this.LastSeqNum = lastSeqNum;
            }
        }

        private void GlobalCommitAction(Event e)
        {
            var lastSeqNum = (e as Coordinator.GlobalCommit).SeqNum;
            this.Assert(this.PendingWriteReq.SeqNum >= lastSeqNum);
            if (this.PendingWriteReq.SeqNum == lastSeqNum)
            {
                if (!this.Data.ContainsKey(this.PendingWriteReq.Idx))
                {
                    this.Data.Add(this.PendingWriteReq.Idx, this.PendingWriteReq.Val);
                }
                else
                {
                    this.Data[this.PendingWriteReq.Idx] = this.PendingWriteReq.Val;
                }

                this.LastSeqNum = lastSeqNum;
            }
        }

        private void HandleReplica(Event e)
        {
            this.PendingWriteReq = (e as Coordinator.ReqReplica).PendingWriteReq;
            this.Assert(this.PendingWriteReq.SeqNum > this.LastSeqNum);
            if (this.Random())
            {
                this.SendEvent(this.Coordinator, new RespReplicaCommit(this.PendingWriteReq.SeqNum));
            }
            else
            {
                this.SendEvent(this.Coordinator, new RespReplicaAbort(this.PendingWriteReq.SeqNum));
            }
        }
    }
}
