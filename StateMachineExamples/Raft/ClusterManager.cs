// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.Coyote;
using Microsoft.Coyote.Actors;

namespace Coyote.Examples.Raft
{
    internal class ClusterManager : StateMachine
    {
        internal class NotifyLeaderUpdate : Event
        {
            public ActorId Leader;
            public int Term;

            public NotifyLeaderUpdate(ActorId leader, int term)
                : base()
            {
                this.Leader = leader;
                this.Term = term;
            }
        }

        internal class RedirectRequest : Event
        {
            public Event Request;

            public RedirectRequest(Event request)
                : base()
            {
                this.Request = request;
            }
        }

        internal class ShutDown : Event { }

        private class LocalEvent : Event { }

        private ActorId[] Servers;
        private int NumberOfServers;

        private ActorId Leader;
        private int LeaderTerm;

        private ActorId Client;

        [Start]
        [OnEntry(nameof(EntryOnInit))]
        [OnEventGotoState(typeof(LocalEvent), typeof(Configuring))]
        private class Init : State { }

        private Transition EntryOnInit()
        {
            this.NumberOfServers = 5;
            this.LeaderTerm = 0;

            this.Servers = new ActorId[this.NumberOfServers];

            for (int idx = 0; idx < this.NumberOfServers; idx++)
            {
                this.Servers[idx] = this.CreateActor(typeof(Server));
            }

            this.Client = this.CreateActor(typeof(Client));

            return this.RaiseEvent(new LocalEvent());
        }

        [OnEntry(nameof(ConfiguringOnInit))]
        [OnEventGotoState(typeof(LocalEvent), typeof(Availability.Unavailable))]
        private class Configuring : State { }

        private Transition ConfiguringOnInit()
        {
            for (int idx = 0; idx < this.NumberOfServers; idx++)
            {
                this.SendEvent(this.Servers[idx], new Server.ConfigureEvent(idx, this.Servers, this.Id));
            }

            this.SendEvent(this.Client, new Client.ConfigureEvent(this.Id));

            return this.RaiseEvent(new LocalEvent());
        }

        private class Availability : StateGroup
        {
            [OnEventDoAction(typeof(NotifyLeaderUpdate), nameof(BecomeAvailable))]
            [OnEventDoAction(typeof(ShutDown), nameof(ShuttingDown))]
            [OnEventGotoState(typeof(LocalEvent), typeof(Available))]
            [DeferEvents(typeof(Client.Request))]
            public class Unavailable : State { }

            [OnEventDoAction(typeof(Client.Request), nameof(SendClientRequestToLeader))]
            [OnEventDoAction(typeof(RedirectRequest), nameof(RedirectClientRequest))]
            [OnEventDoAction(typeof(NotifyLeaderUpdate), nameof(RefreshLeader))]
            [OnEventDoAction(typeof(ShutDown), nameof(ShuttingDown))]
            [OnEventGotoState(typeof(LocalEvent), typeof(Unavailable))]
            public class Available : State { }
        }

        private Transition BecomeAvailable(Event e)
        {
            this.UpdateLeader(e as NotifyLeaderUpdate);
            return this.RaiseEvent(new LocalEvent());
        }

        private void SendClientRequestToLeader(Event e)
        {
            this.SendEvent(this.Leader, e);
        }

        private void RedirectClientRequest(Event e)
        {
            this.SendEvent(this.Id, (e as RedirectRequest).Request);
        }

        private void RefreshLeader(Event e)
        {
            this.UpdateLeader(e as NotifyLeaderUpdate);
        }

        private Transition ShuttingDown()
        {
            for (int idx = 0; idx < this.NumberOfServers; idx++)
            {
                this.SendEvent(this.Servers[idx], new Server.ShutDown());
            }

            return this.Halt();
        }

        /// <summary>
        /// Updates the leader.
        /// </summary>
        /// <param name="request">NotifyLeaderUpdate</param>
        private void UpdateLeader(NotifyLeaderUpdate request)
        {
            if (this.LeaderTerm < request.Term)
            {
                this.Leader = request.Leader;
                this.LeaderTerm = request.Term;
            }
        }
    }
}
