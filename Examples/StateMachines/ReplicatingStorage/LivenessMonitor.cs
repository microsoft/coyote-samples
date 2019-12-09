// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote;
using Microsoft.Coyote.Specifications;

namespace Coyote.Examples.ReplicatingStorage
{
    internal class LivenessMonitor : Monitor
    {
        /// <summary>
        /// Used to configure the liveness monitor.
        /// </summary>
        public class ConfigureEvent : Event
        {
            public int NumberOfReplicas;

            public ConfigureEvent(int numOfReplicas)
                : base()
            {
                this.NumberOfReplicas = numOfReplicas;
            }
        }

        public class NotifyNodeCreated : Event
        {
            public int NodeId;

            public NotifyNodeCreated(int id)
                : base()
            {
                this.NodeId = id;
            }
        }

        public class NotifyNodeFail : Event
        {
            public int NodeId;

            public NotifyNodeFail(int id)
                : base()
            {
                this.NodeId = id;
            }
        }

        public class NotifyNodeUpdate : Event
        {
            public int NodeId;
            public int Data;

            public NotifyNodeUpdate(int id, int data)
                : base()
            {
                this.NodeId = id;
                this.Data = data;
            }
        }

        private class LocalEvent : Event { }

        /// <summary>
        /// Map from node ids to data.
        /// </summary>
        private Dictionary<int, int> DataMap;

        /// <summary>
        /// The number of storage replicas that must
        /// be sustained.
        /// </summary>
        private int NumberOfReplicas;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventDoAction(typeof(ConfigureEvent), nameof(Configure))]
        [OnEventGotoState(typeof(LocalEvent), typeof(Repaired))]
        private class Init : State { }

        private void InitOnEntry()
        {
            this.DataMap = new Dictionary<int, int>();
        }

        private Transition Configure(Event e)
        {
            this.NumberOfReplicas = (e as ConfigureEvent).NumberOfReplicas;
            return this.RaiseEvent(new LocalEvent());
        }

        [Cold]
        [OnEventDoAction(typeof(NotifyNodeCreated), nameof(ProcessNodeCreated))]
        [OnEventDoAction(typeof(NotifyNodeFail), nameof(FailAndCheckRepair))]
        [OnEventDoAction(typeof(NotifyNodeUpdate), nameof(ProcessNodeUpdate))]
        [OnEventGotoState(typeof(LocalEvent), typeof(Repairing))]
        private class Repaired : State { }

        private void ProcessNodeCreated(Event e)
        {
            var nodeId = (e as NotifyNodeCreated).NodeId;
            this.DataMap.Add(nodeId, 0);
        }

        private Transition FailAndCheckRepair(Event e)
        {
            this.ProcessNodeFail(e);
            return this.RaiseEvent(new LocalEvent());
        }

        private void ProcessNodeUpdate(Event e)
        {
            var nodeId = (e as NotifyNodeUpdate).NodeId;
            var data = (e as NotifyNodeUpdate).Data;
            this.DataMap[nodeId] = data;
        }

        [Hot]
        [OnEventDoAction(typeof(NotifyNodeCreated), nameof(ProcessNodeCreated))]
        [OnEventDoAction(typeof(NotifyNodeFail), nameof(ProcessNodeFail))]
        [OnEventDoAction(typeof(NotifyNodeUpdate), nameof(CheckIfRepaired))]
        [OnEventGotoState(typeof(LocalEvent), typeof(Repaired))]
        private class Repairing : State { }

        private void ProcessNodeFail(Event e)
        {
            var nodeId = (e as NotifyNodeFail).NodeId;
            this.DataMap.Remove(nodeId);
        }

        private Transition CheckIfRepaired(Event e)
        {
            this.ProcessNodeUpdate(e);
            var consensus = this.DataMap.Select(kvp => kvp.Value).GroupBy(v => v).
                OrderByDescending(v => v.Count()).FirstOrDefault();

            var numOfReplicas = consensus.Count();
            if (numOfReplicas >= this.NumberOfReplicas)
            {
                return this.RaiseEvent(new LocalEvent());
            }

            return default;
        }
    }
}
