// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.Coyote;
using Microsoft.Coyote.Actors;

namespace Coyote.Examples.ReplicatingStorage
{
    internal class Client : StateMachine
    {
        /// <summary>
        /// Used to configure the client.
        /// </summary>
        public class ConfigureEvent : Event
        {
            public ActorId NodeManager;

            public ConfigureEvent(ActorId manager)
                : base()
            {
                this.NodeManager = manager;
            }
        }

        /// <summary>
        /// Used for a client request.
        /// </summary>
        internal class Request : Event
        {
            public ActorId Client;
            public int Command;

            public Request(ActorId client, int cmd)
                : base()
            {
                this.Client = client;
                this.Command = cmd;
            }
        }

        private class LocalEvent : Event { }

        private ActorId NodeManager;

        private int Counter;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventDoAction(typeof(ConfigureEvent), nameof(Configure))]
        [OnEventGotoState(typeof(LocalEvent), typeof(PumpRequest))]
        private class Init : State { }

        private void InitOnEntry()
        {
            this.Counter = 0;
        }

        private Transition Configure(Event e)
        {
            this.NodeManager = (e as ConfigureEvent).NodeManager;
            return this.RaiseEvent(new LocalEvent());
        }

        [OnEntry(nameof(PumpRequestOnEntry))]
        [OnEventGotoState(typeof(LocalEvent), typeof(PumpRequest))]
        private class PumpRequest : State { }

        private Transition PumpRequestOnEntry()
        {
            int command = this.RandomInteger(100) + 1;
            this.Counter++;

            this.Logger.WriteLine("\n [Client] new request {0}.\n", command);

            this.SendEvent(this.NodeManager, new Request(this.Id, command));

            if (this.Counter == 1)
            {
                return this.Halt();
            }

            return this.RaiseEvent(new LocalEvent());
        }
    }
}
