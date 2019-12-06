// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Coyote;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Specifications;

namespace Coyote.Examples.ChainReplication
{
    internal class ServerResponseSeqMonitor : Monitor
    {
        internal class Config : Event
        {
            public List<ActorId> Servers;

            public Config(List<ActorId> servers)
                : base()
            {
                this.Servers = servers;
            }
        }

        internal class UpdateServers : Event
        {
            public List<ActorId> Servers;

            public UpdateServers(List<ActorId> servers)
                : base()
            {
                this.Servers = servers;
            }
        }

        internal class ResponseToUpdate : Event
        {
            public ActorId Tail;
            public int Key;
            public int Value;

            public ResponseToUpdate(ActorId tail, int key, int val)
                : base()
            {
                this.Tail = tail;
                this.Key = key;
                this.Value = val;
            }
        }

        internal class ResponseToQuery : Event
        {
            public ActorId Tail;
            public int Key;
            public int Value;

            public ResponseToQuery(ActorId tail, int key, int val)
                : base()
            {
                this.Tail = tail;
                this.Key = key;
                this.Value = val;
            }
        }

        private class Local : Event { }

        private List<ActorId> Servers;
        private Dictionary<int, int> LastUpdateResponse;

        [Start]
        [OnEventGotoState(typeof(Local), typeof(Wait))]
        [OnEventDoAction(typeof(Config), nameof(Configure))]
        private class Init : State { }

        private Transition Configure(Event e)
        {
            this.Servers = (e as Config).Servers;
            this.LastUpdateResponse = new Dictionary<int, int>();
            return this.RaiseEvent(new Local());
        }

        [OnEventDoAction(typeof(ResponseToUpdate), nameof(ResponseToUpdateAction))]
        [OnEventDoAction(typeof(ResponseToQuery), nameof(ResponseToQueryAction))]
        [OnEventDoAction(typeof(UpdateServers), nameof(ProcessUpdateServers))]
        private class Wait : State { }

        private void ResponseToUpdateAction(Event e)
        {
            var tail = (e as ResponseToUpdate).Tail;
            var key = (e as ResponseToUpdate).Key;
            var value = (e as ResponseToUpdate).Value;

            if (this.Servers.Contains(tail))
            {
                if (this.LastUpdateResponse.ContainsKey(key))
                {
                    this.LastUpdateResponse[key] = value;
                }
                else
                {
                    this.LastUpdateResponse.Add(key, value);
                }
            }
        }

        private void ResponseToQueryAction(Event e)
        {
            var tail = (e as ResponseToQuery).Tail;
            var key = (e as ResponseToQuery).Key;
            var value = (e as ResponseToQuery).Value;

            if (this.Servers.Contains(tail))
            {
                this.Assert(value == this.LastUpdateResponse[key], "Value {0} is not " +
                    "equal to {1}", value, this.LastUpdateResponse[key]);
            }
        }

        private void ProcessUpdateServers(Event e)
        {
            this.Servers = (e as UpdateServers).Servers;
        }
    }
}
