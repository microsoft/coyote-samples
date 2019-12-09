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

        private int ClientId;

        private ActorId HeadNode;
        private ActorId TailNode;

        private int StartIn;
        private int Next;

        private Dictionary<int, int> KeyValueStore;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(Local), typeof(PumpUpdateRequests))]
        private class Init : State { }

        private Transition InitOnEntry(Event e)
        {
            this.ClientId = (e as Config).Id;

            this.HeadNode = (e as Config).HeadNode;
            this.TailNode = (e as Config).TailNode;

            this.StartIn = (e as Config).Value;
            this.Next = 1;

            this.KeyValueStore = new Dictionary<int, int>
            {
                { 1 * this.StartIn, 100 },
                { 2 * this.StartIn, 200 },
                { 3 * this.StartIn, 300 },
                { 4 * this.StartIn, 400 }
            };

            return this.RaiseEvent(new Local());
        }

        [OnEntry(nameof(PumpUpdateRequestsOnEntry))]
        [OnEventGotoState(typeof(Local), typeof(PumpUpdateRequests), nameof(PumpRequestsLocalAction))]
        [OnEventGotoState(typeof(Done), typeof(PumpQueryRequests), nameof(PumpRequestsDoneAction))]
        [IgnoreEvents(typeof(ChainReplicationServer.ResponseToUpdate),
            typeof(ChainReplicationServer.ResponseToQuery))]
        private class PumpUpdateRequests : State { }

        private Transition PumpUpdateRequestsOnEntry()
        {
            this.SendEvent(this.HeadNode, new Update(this.Id, this.Next * this.StartIn,
                this.KeyValueStore[this.Next * this.StartIn]));

            if (this.Next >= 3)
            {
                return this.RaiseEvent(new Done());
            }

            return this.RaiseEvent(new Local());
        }

        [OnEntry(nameof(PumpQueryRequestsOnEntry))]
        [OnEventGotoState(typeof(Local), typeof(PumpQueryRequests), nameof(PumpRequestsLocalAction))]
        [IgnoreEvents(typeof(ChainReplicationServer.ResponseToUpdate),
            typeof(ChainReplicationServer.ResponseToQuery))]
        private class PumpQueryRequests : State { }

        private Transition PumpQueryRequestsOnEntry()
        {
            this.SendEvent(this.TailNode, new Query(this.Id, this.Next * this.StartIn));

            if (this.Next >= 3)
            {
                return this.Halt();
            }

            return this.RaiseEvent(new Local());
        }

        private void PumpRequestsLocalAction()
        {
            this.Next++;
        }

        private void PumpRequestsDoneAction()
        {
            this.Next = 1;
        }
    }
}
