// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.Coyote;
using Microsoft.Coyote.Actors;

namespace Coyote.Examples.Raft
{
    internal class Client : StateMachine
    {
        #region events

        /// <summary>
        /// Used to configure the client.
        /// </summary>
        public class ConfigureEvent : Event
        {
            public ActorId Cluster;

            public ConfigureEvent(ActorId cluster)
                : base()
            {
                this.Cluster = cluster;
            }
        }

        /// <summary>
        /// Used for a client request.
        /// </summary>
        internal class Request : Event
        {
            public ActorId Client;
            public int Command;

            public Request(ActorId client, int command)
                : base()
            {
                this.Client = client;
                this.Command = command;
            }
        }

        internal class Response : Event { }

        private class LocalEvent : Event { }

        #endregion

        #region fields

        private ActorId Cluster;

        private int LatestCommand;
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
            this.LatestCommand = -1;
            this.Counter = 0;
        }

        private void Configure()
        {
            this.Cluster = (this.ReceivedEvent as ConfigureEvent).Cluster;
            this.RaiseEvent(new LocalEvent());
        }

        [OnEntry(nameof(PumpRequestOnEntry))]
        [OnEventDoAction(typeof(Response), nameof(ProcessResponse))]
        [OnEventGotoState(typeof(LocalEvent), typeof(PumpRequest))]
        private class PumpRequest : State { }

        private void PumpRequestOnEntry()
        {
            this.LatestCommand = this.RandomInteger(100);
            this.Counter++;

            this.Logger.WriteLine("\n [Client] new request " + this.LatestCommand + "\n");

            this.SendEvent(this.Cluster, new Request(this.Id, this.LatestCommand));
        }

        private void ProcessResponse()
        {
            if (this.Counter == 3)
            {
                this.SendEvent(this.Cluster, new ClusterManager.ShutDown());
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
