// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.Coyote;
using Microsoft.Coyote.Actors;

namespace Coyote.Examples.ReplicatingStorage
{
    internal class StorageNode : StateMachine
    {
        /// <summary>
        /// Used to configure the storage node.
        /// </summary>
        public class ConfigureEvent : Event
        {
            public ActorId Environment;
            public ActorId NodeManager;
            public int Id;

            public ConfigureEvent(ActorId env, ActorId manager, int id)
                : base()
            {
                this.Environment = env;
                this.NodeManager = manager;
                this.Id = id;
            }
        }

        public class StoreRequest : Event
        {
            public int Command;

            public StoreRequest(int cmd)
                : base()
            {
                this.Command = cmd;
            }
        }

        public class SyncReport : Event
        {
            public int NodeId;
            public int Data;

            public SyncReport(int id, int data)
                : base()
            {
                this.NodeId = id;
                this.Data = data;
            }
        }

        public class SyncRequest : Event
        {
            public int Data;

            public SyncRequest(int data)
                : base()
            {
                this.Data = data;
            }
        }

        internal class ShutDown : Event { }

        private class LocalEvent : Event { }

        /// <summary>
        /// The environment.
        /// </summary>
        private ActorId Environment;

        /// <summary>
        /// The storage node manager.
        /// </summary>
        private ActorId NodeManager;

        /// <summary>
        /// The storage node id.
        /// </summary>
        private int NodeId;

        /// <summary>
        /// The data that this storage node contains.
        /// </summary>
        private int Data;

        /// <summary>
        /// The sync report timer.
        /// </summary>
        private ActorId SyncTimer;

        [Start]
        [OnEntry(nameof(EntryOnInit))]
        [OnEventDoAction(typeof(ConfigureEvent), nameof(Configure))]
        [OnEventGotoState(typeof(LocalEvent), typeof(Active))]
        [DeferEvents(typeof(SyncTimer.TimeoutEvent))]
        private class Init : State { }

        private void EntryOnInit()
        {
            this.Data = 0;
            this.SyncTimer = this.CreateActor(typeof(SyncTimer));
            this.SendEvent(this.SyncTimer, new SyncTimer.ConfigureEvent(this.Id));
        }

        private Transition Configure(Event e)
        {
            this.Environment = (e as ConfigureEvent).Environment;
            this.NodeManager = (e as ConfigureEvent).NodeManager;
            this.NodeId = (e as ConfigureEvent).Id;

            this.Logger.WriteLine("\n [StorageNode-{0}] is up and running.\n", this.NodeId);

            this.Monitor<LivenessMonitor>(new LivenessMonitor.NotifyNodeCreated(this.NodeId));
            this.SendEvent(this.Environment, new Environment.NotifyNode(this.Id));

            return this.RaiseEvent(new LocalEvent());
        }

        [OnEventDoAction(typeof(StoreRequest), nameof(Store))]
        [OnEventDoAction(typeof(SyncRequest), nameof(Sync))]
        [OnEventDoAction(typeof(SyncTimer.TimeoutEvent), nameof(GenerateSyncReport))]
        [OnEventDoAction(typeof(Environment.FaultInject), nameof(Terminate))]
        private class Active : State { }

        private void Store(Event e)
        {
            var cmd = (e as StoreRequest).Command;
            this.Data += cmd;
            this.Logger.WriteLine("\n [StorageNode-{0}] is applying command {1}.\n", this.NodeId, cmd);
            this.Monitor<LivenessMonitor>(new LivenessMonitor.NotifyNodeUpdate(this.NodeId, this.Data));
        }

        private void Sync(Event e)
        {
            var data = (e as SyncRequest).Data;
            this.Data = data;
            this.Logger.WriteLine("\n [StorageNode-{0}] is syncing with data {1}.\n", this.NodeId, this.Data);
            this.Monitor<LivenessMonitor>(new LivenessMonitor.NotifyNodeUpdate(this.NodeId, this.Data));
        }

        private void GenerateSyncReport()
        {
            this.SendEvent(this.NodeManager, new SyncReport(this.NodeId, this.Data));
        }

        private Transition Terminate()
        {
            this.Monitor<LivenessMonitor>(new LivenessMonitor.NotifyNodeFail(this.NodeId));
            this.SendEvent(this.SyncTimer, HaltEvent.Instance);
            return this.Halt();
        }
    }
}
