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
        #region events

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

        #endregion

        #region fields

        private ActorId NodeManager;

        private int Counter;

        #endregion

        #region states

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventDoAction(typeof(ConfigureEvent), nameof(Configure))]
        [OnEventGotoState(typeof(LocalEvent), typeof(PumpRequest))]
        private class Init : State { }

        private void InitOnEntry()
        {
            this.Counter = 0;
        }

        private void Configure()
        {
            this.NodeManager = (this.ReceivedEvent as ConfigureEvent).NodeManager;
            this.RaiseEvent(new LocalEvent());
        }

        [OnEntry(nameof(PumpRequestOnEntry))]
        [OnEventGotoState(typeof(LocalEvent), typeof(PumpRequest))]
        private class PumpRequest : State { }

        private void PumpRequestOnEntry()
        {
            int command = this.RandomInteger(100) + 1;
            this.Counter++;

            this.Logger.WriteLine("\n [Client] new request {0}.\n", command);

            this.SendEvent(this.NodeManager, new Request(this.Id, command));

            if (this.Counter == 1)
            {
                this.RaiseEvent(new HaltEvent());
            }
            else
            {
                this.RaiseEvent(new LocalEvent());
            }
        }

        #endregion
    }
}
