// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Coyote;
using Microsoft.Coyote.Actors;

namespace Coyote.Examples.ReplicatingStorage
{
    internal class Environment : StateMachine
    {
        public class NotifyNode : Event
        {
            public ActorId Node;

            public NotifyNode(ActorId node)
                : base()
            {
                this.Node = node;
            }
        }

        public class FaultInject : Event { }

        private class CreateFailure : Event { }

        private class LocalEvent : Event { }

        private ActorId NodeManager;
        private int NumberOfReplicas;

        private List<ActorId> AliveNodes;
        private int NumberOfFaults;

        private ActorId Client;

        /// <summary>
        /// The failure timer.
        /// </summary>
        private ActorId FailureTimer;

        [Start]
        [OnEntry(nameof(EntryOnInit))]
        [OnEventGotoState(typeof(LocalEvent), typeof(Configuring))]
        private class Init : State { }

        private Transition EntryOnInit()
        {
            this.NumberOfReplicas = 3;
            this.NumberOfFaults = 1;
            this.AliveNodes = new List<ActorId>();

            this.Monitor<LivenessMonitor>(new LivenessMonitor.ConfigureEvent(this.NumberOfReplicas));

            this.NodeManager = this.CreateActor(typeof(NodeManager));
            this.Client = this.CreateActor(typeof(Client));

            return this.RaiseEvent(new LocalEvent());
        }

        [OnEntry(nameof(ConfiguringOnInit))]
        [OnEventGotoState(typeof(LocalEvent), typeof(Active))]
        [DeferEvents(typeof(FailureTimer.TimeoutEvent))]
        private class Configuring : State { }

        private Transition ConfiguringOnInit()
        {
            this.SendEvent(this.NodeManager, new NodeManager.ConfigureEvent(this.Id, this.NumberOfReplicas));
            this.SendEvent(this.Client, new Client.ConfigureEvent(this.NodeManager));
            return this.RaiseEvent(new LocalEvent());
        }

        [OnEventDoAction(typeof(NotifyNode), nameof(UpdateAliveNodes))]
        [OnEventDoAction(typeof(FailureTimer.TimeoutEvent), nameof(InjectFault))]
        private class Active : State { }

        private void UpdateAliveNodes(Event e)
        {
            var node = (e as NotifyNode).Node;
            this.AliveNodes.Add(node);

            if (this.AliveNodes.Count == this.NumberOfReplicas &&
                this.FailureTimer == null)
            {
                this.FailureTimer = this.CreateActor(typeof(FailureTimer));
                this.SendEvent(this.FailureTimer, new FailureTimer.ConfigureEvent(this.Id));
            }
        }

        private void InjectFault()
        {
            if (this.NumberOfFaults == 0 ||
                this.AliveNodes.Count == 0)
            {
                return;
            }

            int nodeId = this.RandomInteger(this.AliveNodes.Count);
            var node = this.AliveNodes[nodeId];

            this.Logger.WriteLine("\n [Environment] injecting fault.\n");

            this.SendEvent(node, new FaultInject());
            this.SendEvent(this.NodeManager, new NodeManager.NotifyFailure(node));
            this.AliveNodes.Remove(node);

            this.NumberOfFaults--;
            if (this.NumberOfFaults == 0)
            {
                this.SendEvent(this.FailureTimer, HaltEvent.Instance);
            }
        }
    }
}
