// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Coyote;
using Microsoft.Coyote.Actors;

namespace Coyote.Examples.ChainReplication
{
    internal class ChainReplicationServer : StateMachine
    {
        internal class Config : Event
        {
            public int Id;
            public bool IsHead;
            public bool IsTail;

            public Config(int id, bool isHead, bool isTail)
                : base()
            {
                this.Id = id;
                this.IsHead = isHead;
                this.IsTail = isTail;
            }
        }

        internal class PredSucc : Event
        {
            public ActorId Predecessor;
            public ActorId Successor;

            public PredSucc(ActorId pred, ActorId succ)
                : base()
            {
                this.Predecessor = pred;
                this.Successor = succ;
            }
        }

        internal class ForwardUpdate : Event
        {
            public ActorId Predecessor;
            public int NextSeqId;
            public ActorId Client;
            public int Key;
            public int Value;

            public ForwardUpdate(ActorId pred, int nextSeqId, ActorId client, int key, int val)
                : base()
            {
                this.Predecessor = pred;
                this.NextSeqId = nextSeqId;
                this.Client = client;
                this.Key = key;
                this.Value = val;
            }
        }

        internal class BackwardAck : Event
        {
            public int NextSeqId;

            public BackwardAck(int nextSeqId)
                : base()
            {
                this.NextSeqId = nextSeqId;
            }
        }

        internal class NewPredecessor : Event
        {
            public ActorId Master;
            public ActorId Predecessor;

            public NewPredecessor(ActorId master, ActorId pred)
                : base()
            {
                this.Master = master;
                this.Predecessor = pred;
            }
        }

        internal class NewSuccessor : Event
        {
            public ActorId Master;
            public ActorId Successor;
            public int LastUpdateReceivedSucc;
            public int LastAckSent;

            public NewSuccessor(ActorId master, ActorId succ,
                int lastUpdateReceivedSucc, int lastAckSent)
                : base()
            {
                this.Master = master;
                this.Successor = succ;
                this.LastUpdateReceivedSucc = lastUpdateReceivedSucc;
                this.LastAckSent = lastAckSent;
            }
        }

        internal class NewSuccInfo : Event
        {
            public int LastUpdateReceivedSucc;
            public int LastAckSent;

            public NewSuccInfo(int lastUpdateReceivedSucc, int lastAckSent)
                : base()
            {
                this.LastUpdateReceivedSucc = lastUpdateReceivedSucc;
                this.LastAckSent = lastAckSent;
            }
        }

        internal class ResponseToQuery : Event
        {
            public int Value;

            public ResponseToQuery(int val)
                : base()
            {
                this.Value = val;
            }
        }

        internal class ResponseToUpdate : Event { }

        private class Local : Event { }

        private int ServerId;
        private bool IsHead;
        private bool IsTail;

        private ActorId Predecessor;
        private ActorId Successor;

        private Dictionary<int, int> KeyValueStore;
        private List<int> History;
        private List<SentLog> SentHistory;

        private int NextSeqId;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(Local), typeof(WaitForRequest))]
        [OnEventDoAction(typeof(PredSucc), nameof(SetupPredSucc))]
        [DeferEvents(typeof(Client.Update), typeof(Client.Query),
            typeof(BackwardAck), typeof(ForwardUpdate))]
        private class Init : State { }

        private void InitOnEntry(Event e)
        {
            this.ServerId = (e as Config).Id;
            this.IsHead = (e as Config).IsHead;
            this.IsTail = (e as Config).IsTail;

            this.KeyValueStore = new Dictionary<int, int>();
            this.History = new List<int>();
            this.SentHistory = new List<SentLog>();

            this.NextSeqId = 0;
        }

        private Transition SetupPredSucc(Event e)
        {
            this.Predecessor = (e as PredSucc).Predecessor;
            this.Successor = (e as PredSucc).Successor;
            return this.RaiseEvent(new Local());
        }

        [OnEventGotoState(typeof(Client.Update), typeof(ProcessUpdate), nameof(ProcessUpdateAction))]
        [OnEventGotoState(typeof(ForwardUpdate), typeof(ProcessFwdUpdate))]
        [OnEventGotoState(typeof(BackwardAck), typeof(ProcessBckAck))]
        [OnEventDoAction(typeof(Client.Query), nameof(ProcessQueryAction))]
        [OnEventDoAction(typeof(NewPredecessor), nameof(UpdatePredecessor))]
        [OnEventDoAction(typeof(NewSuccessor), nameof(UpdateSuccessor))]
        [OnEventDoAction(typeof(ChainReplicationMaster.BecomeHead), nameof(ProcessBecomeHead))]
        [OnEventDoAction(typeof(ChainReplicationMaster.BecomeTail), nameof(ProcessBecomeTail))]
        [OnEventDoAction(typeof(FailureDetector.Ping), nameof(SendPong))]
        private class WaitForRequest : State { }

        private void ProcessUpdateAction()
        {
            this.NextSeqId++;
            this.Assert(this.IsHead, "Server {0} is not head", this.ServerId);
        }

        private void ProcessQueryAction(Event e)
        {
            var client = (e as Client.Query).Client;
            var key = (e as Client.Query).Key;

            this.Assert(this.IsTail, "Server {0} is not tail", this.Id);

            if (this.KeyValueStore.ContainsKey(key))
            {
                this.Monitor<ServerResponseSeqMonitor>(new ServerResponseSeqMonitor.ResponseToQuery(
                    this.Id, key, this.KeyValueStore[key]));

                this.SendEvent(client, new ResponseToQuery(this.KeyValueStore[key]));
            }
            else
            {
                this.SendEvent(client, new ResponseToQuery(-1));
            }
        }

        private void ProcessBecomeHead(Event e)
        {
            this.IsHead = true;
            this.Predecessor = this.Id;

            var target = (e as ChainReplicationMaster.BecomeHead).Target;
            this.SendEvent(target, new ChainReplicationMaster.HeadChanged());
        }

        private void ProcessBecomeTail(Event e)
        {
            this.IsTail = true;
            this.Successor = this.Id;

            for (int i = 0; i < this.SentHistory.Count; i++)
            {
                this.Monitor<ServerResponseSeqMonitor>(new ServerResponseSeqMonitor.ResponseToUpdate(
                    this.Id, this.SentHistory[i].Key, this.SentHistory[i].Value));

                this.SendEvent(this.SentHistory[i].Client, new ResponseToUpdate());
                this.SendEvent(this.Predecessor, new BackwardAck(this.SentHistory[i].NextSeqId));
            }

            var target = (e as ChainReplicationMaster.BecomeTail).Target;
            this.SendEvent(target, new ChainReplicationMaster.TailChanged());
        }

        private void SendPong(Event e)
        {
            var target = (e as FailureDetector.Ping).Target;
            this.SendEvent(target, new FailureDetector.Pong());
        }

        private void UpdatePredecessor(Event e)
        {
            var master = (e as NewPredecessor).Master;
            this.Predecessor = (e as NewPredecessor).Predecessor;

            if (this.History.Count > 0)
            {
                if (this.SentHistory.Count > 0)
                {
                    this.SendEvent(master, new NewSuccInfo(this.History[this.History.Count - 1],
                        this.SentHistory[0].NextSeqId));
                }
                else
                {
                    this.SendEvent(master, new NewSuccInfo(this.History[this.History.Count - 1],
                        this.History[this.History.Count - 1]));
                }
            }
        }

        private void UpdateSuccessor(Event e)
        {
            var master = (e as NewSuccessor).Master;
            this.Successor = (e as NewSuccessor).Successor;
            var lastUpdateReceivedSucc = (e as NewSuccessor).LastUpdateReceivedSucc;
            var lastAckSent = (e as NewSuccessor).LastAckSent;

            if (this.SentHistory.Count > 0)
            {
                for (int i = 0; i < this.SentHistory.Count; i++)
                {
                    if (this.SentHistory[i].NextSeqId > lastUpdateReceivedSucc)
                    {
                        this.SendEvent(this.Successor, new ForwardUpdate(this.Id, this.SentHistory[i].NextSeqId,
                            this.SentHistory[i].Client, this.SentHistory[i].Key, this.SentHistory[i].Value));
                    }
                }

                int tempIndex = -1;
                for (int i = this.SentHistory.Count - 1; i >= 0; i--)
                {
                    if (this.SentHistory[i].NextSeqId == lastAckSent)
                    {
                        tempIndex = i;
                    }
                }

                for (int i = 0; i < tempIndex; i++)
                {
                    this.SendEvent(this.Predecessor, new BackwardAck(this.SentHistory[0].NextSeqId));
                    this.SentHistory.RemoveAt(0);
                }
            }

            this.SendEvent(master, new ChainReplicationMaster.Success());
        }

        [OnEntry(nameof(ProcessUpdateOnEntry))]
        [OnEventGotoState(typeof(Local), typeof(WaitForRequest))]
        private class ProcessUpdate : State { }

        private Transition ProcessUpdateOnEntry(Event e)
        {
            var client = (e as Client.Update).Client;
            var key = (e as Client.Update).Key;
            var value = (e as Client.Update).Value;

            if (this.KeyValueStore.ContainsKey(key))
            {
                this.KeyValueStore[key] = value;
            }
            else
            {
                this.KeyValueStore.Add(key, value);
            }

            this.History.Add(this.NextSeqId);

            this.Monitor<InvariantMonitor>(
                new InvariantMonitor.HistoryUpdate(this.Id, new List<int>(this.History)));

            this.SentHistory.Add(new SentLog(this.NextSeqId, client, key, value));
            this.Monitor<InvariantMonitor>(
                new InvariantMonitor.SentUpdate(this.Id, new List<SentLog>(this.SentHistory)));

            this.SendEvent(this.Successor, new ForwardUpdate(this.Id, this.NextSeqId, client, key, value));

            return this.RaiseEvent(new Local());
        }

        [OnEntry(nameof(ProcessFwdUpdateOnEntry))]
        [OnEventGotoState(typeof(Local), typeof(WaitForRequest))]
        private class ProcessFwdUpdate : State { }

        private Transition ProcessFwdUpdateOnEntry(Event e)
        {
            var pred = (e as ForwardUpdate).Predecessor;
            var nextSeqId = (e as ForwardUpdate).NextSeqId;
            var client = (e as ForwardUpdate).Client;
            var key = (e as ForwardUpdate).Key;
            var value = (e as ForwardUpdate).Value;

            if (pred.Equals(this.Predecessor))
            {
                this.NextSeqId = nextSeqId;

                if (this.KeyValueStore.ContainsKey(key))
                {
                    this.KeyValueStore[key] = value;
                }
                else
                {
                    this.KeyValueStore.Add(key, value);
                }

                if (!this.IsTail)
                {
                    this.History.Add(nextSeqId);

                    this.Monitor<InvariantMonitor>(
                        new InvariantMonitor.HistoryUpdate(this.Id, new List<int>(this.History)));

                    this.SentHistory.Add(new SentLog(this.NextSeqId, client, key, value));
                    this.Monitor<InvariantMonitor>(
                        new InvariantMonitor.SentUpdate(this.Id, new List<SentLog>(this.SentHistory)));

                    this.SendEvent(this.Successor, new ForwardUpdate(this.Id, this.NextSeqId, client, key, value));
                }
                else
                {
                    if (!this.IsHead)
                    {
                        this.History.Add(nextSeqId);
                    }

                    this.Monitor<ServerResponseSeqMonitor>(new ServerResponseSeqMonitor.ResponseToUpdate(
                        this.Id, key, value));

                    this.SendEvent(client, new ResponseToUpdate());
                    this.SendEvent(this.Predecessor, new BackwardAck(nextSeqId));
                }
            }

            return this.RaiseEvent(new Local());
        }

        [OnEntry(nameof(ProcessBckAckOnEntry))]
        [OnEventGotoState(typeof(Local), typeof(WaitForRequest))]
        private class ProcessBckAck : State { }

        private Transition ProcessBckAckOnEntry(Event e)
        {
            var nextSeqId = (e as BackwardAck).NextSeqId;

            this.RemoveItemFromSent(nextSeqId);

            if (!this.IsHead)
            {
                this.SendEvent(this.Predecessor, new BackwardAck(nextSeqId));
            }

            return this.RaiseEvent(new Local());
        }

        private void RemoveItemFromSent(int seqId)
        {
            int removeIdx = -1;

            for (int i = this.SentHistory.Count - 1; i >= 0; i--)
            {
                if (seqId == this.SentHistory[i].NextSeqId)
                {
                    removeIdx = i;
                }
            }

            if (removeIdx != -1)
            {
                this.SentHistory.RemoveAt(removeIdx);
            }
        }
    }
}
