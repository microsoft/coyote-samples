// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Coyote;
using Microsoft.Coyote.Actors;

namespace Coyote.Examples.ChainReplication
{
    internal class Client : StateMachine
    {
        #region events

        internal class Config : Event
        {
            public int Id;
            public ActorId HeadNode;
            public ActorId TailNode;
            public int Value;

            public Config(int id, ActorId head, ActorId tail, int val)
                : base()
            {
                this.Id = id;
                this.HeadNode = head;
                this.TailNode = tail;
                this.Value = val;
            }
        }

        internal class UpdateHeadTail : Event
        {
            public ActorId Head;
            public ActorId Tail;

            public UpdateHeadTail(ActorId head, ActorId tail)
                : base()
            {
                this.Head = head;
                this.Tail = tail;
            }
        }

        internal class Update : Event
        {
            public ActorId Client;
            public int Key;
            public int Value;

            public Update(ActorId client, int key, int value)
                : base()
            {
                this.Client = client;
                this.Key = key;
                this.Value = value;
            }
        }

        internal class Query : Event
        {
            public ActorId Client;
            public int Key;

            public Query(ActorId client, int key)
                : base()
            {
                this.Client = client;
                this.Key = key;
            }
        }

        private class Local : Event { }

        private class Done : Event { }

        #endregion

        #region fields

        private int ClientId;

        private ActorId HeadNode;
        private ActorId TailNode;

        private int StartIn;
        private int Next;

        private Dictionary<int, int> KeyValueStore;

        #endregion

        #region states

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(Local), typeof(PumpUpdateRequests))]
        private class Init : State { }

        private void InitOnEntry()
        {
            this.ClientId = (this.ReceivedEvent as Config).Id;

            this.HeadNode = (this.ReceivedEvent as Config).HeadNode;
            this.TailNode = (this.ReceivedEvent as Config).TailNode;

            this.StartIn = (this.ReceivedEvent as Config).Value;
            this.Next = 1;

            this.KeyValueStore = new Dictionary<int, int>();
            this.KeyValueStore.Add(1 * this.StartIn, 100);
            this.KeyValueStore.Add(2 * this.StartIn, 200);
            this.KeyValueStore.Add(3 * this.StartIn, 300);
            this.KeyValueStore.Add(4 * this.StartIn, 400);

            this.RaiseEvent(new Local());
        }

        [OnEntry(nameof(PumpUpdateRequestsOnEntry))]
        [OnEventGotoState(typeof(Local), typeof(PumpUpdateRequests), nameof(PumpRequestsLocalAction))]
        [OnEventGotoState(typeof(Done), typeof(PumpQueryRequests), nameof(PumpRequestsDoneAction))]
        [IgnoreEvents(typeof(ChainReplicationServer.ResponseToUpdate),
            typeof(ChainReplicationServer.ResponseToQuery))]
        private class PumpUpdateRequests : State { }

        private void PumpUpdateRequestsOnEntry()
        {
            this.SendEvent(this.HeadNode, new Update(this.Id, this.Next * this.StartIn,
                this.KeyValueStore[this.Next * this.StartIn]));

            if (this.Next >= 3)
            {
                this.RaiseEvent(new Done());
            }
            else
            {
                this.RaiseEvent(new Local());
            }
        }

        [OnEntry(nameof(PumpQueryRequestsOnEntry))]
        [OnEventGotoState(typeof(Local), typeof(PumpQueryRequests), nameof(PumpRequestsLocalAction))]
        [IgnoreEvents(typeof(ChainReplicationServer.ResponseToUpdate),
            typeof(ChainReplicationServer.ResponseToQuery))]
        private class PumpQueryRequests : State { }

        private void PumpQueryRequestsOnEntry()
        {
            this.SendEvent(this.TailNode, new Query(this.Id, this.Next * this.StartIn));

            if (this.Next >= 3)
            {
                this.RaiseEvent(new HaltEvent());
            }
            else
            {
                this.RaiseEvent(new Local());
            }
        }

        private void PumpRequestsLocalAction()
        {
            this.Next++;
        }

        private void PumpRequestsDoneAction()
        {
            this.Next = 1;
        }

        #endregion
    }
}
