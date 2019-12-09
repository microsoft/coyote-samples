// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote;
using Microsoft.Coyote.Actors;

namespace Coyote.Examples.Chord
{
    internal class ChordNode : StateMachine
    {
        internal class Config : Event
        {
            public int Id;
            public HashSet<int> Keys;
            public List<ActorId> Nodes;
            public List<int> NodeIds;
            public ActorId Manager;

            public Config(int id, HashSet<int> keys, List<ActorId> nodes,
                List<int> nodeIds, ActorId manager)
                : base()
            {
                this.Id = id;
                this.Keys = keys;
                this.Nodes = nodes;
                this.NodeIds = nodeIds;
                this.Manager = manager;
            }
        }

        internal class Join : Event
        {
            public int Id;
            public List<ActorId> Nodes;
            public List<int> NodeIds;
            public int NumOfIds;
            public ActorId Manager;

            public Join(int id, List<ActorId> nodes, List<int> nodeIds,
                int numOfIds, ActorId manager)
                : base()
            {
                this.Id = id;
                this.Nodes = nodes;
                this.NodeIds = nodeIds;
                this.NumOfIds = numOfIds;
                this.Manager = manager;
            }
        }

        internal class FindSuccessor : Event
        {
            public ActorId Sender;
            public int Key;

            public FindSuccessor(ActorId sender, int key)
                : base()
            {
                this.Sender = sender;
                this.Key = key;
            }
        }

        internal class FindSuccessorResp : Event
        {
            public ActorId Node;
            public int Key;

            public FindSuccessorResp(ActorId node, int key)
                : base()
            {
                this.Node = node;
                this.Key = key;
            }
        }

        internal class FindPredecessor : Event
        {
            public ActorId Sender;

            public FindPredecessor(ActorId sender)
                : base()
            {
                this.Sender = sender;
            }
        }

        internal class FindPredecessorResp : Event
        {
            public ActorId Node;

            public FindPredecessorResp(ActorId node)
                : base()
            {
                this.Node = node;
            }
        }

        internal class QueryId : Event
        {
            public ActorId Sender;

            public QueryId(ActorId sender)
                : base()
            {
                this.Sender = sender;
            }
        }

        internal class QueryIdResp : Event
        {
            public int Id;

            public QueryIdResp(int id)
                : base()
            {
                this.Id = id;
            }
        }

        internal class AskForKeys : Event
        {
            public ActorId Node;
            public int Id;

            public AskForKeys(ActorId node, int id)
                : base()
            {
                this.Node = node;
                this.Id = id;
            }
        }

        internal class AskForKeysResp : Event
        {
            public List<int> Keys;

            public AskForKeysResp(List<int> keys)
                : base()
            {
                this.Keys = keys;
            }
        }

        private class NotifySuccessor : Event
        {
            public ActorId Node;

            public NotifySuccessor(ActorId node)
                : base()
            {
                this.Node = node;
            }
        }

        internal class JoinAck : Event { }

        internal class Stabilize : Event { }

        internal class Terminate : Event { }

        private class Local : Event { }

        private int NodeId;
        private HashSet<int> Keys;
        private int NumOfIds;

        private Dictionary<int, Finger> FingerTable;
        private ActorId Predecessor;

        private ActorId Manager;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(Local), typeof(Waiting))]
        [OnEventDoAction(typeof(Config), nameof(Configure))]
        [OnEventDoAction(typeof(Join), nameof(JoinCluster))]
        [DeferEvents(typeof(AskForKeys), typeof(NotifySuccessor), typeof(Stabilize))]
        private class Init : State { }

        private void InitOnEntry()
        {
            this.FingerTable = new Dictionary<int, Finger>();
        }

        private Transition Configure(Event e)
        {
            this.NodeId = (e as Config).Id;
            this.Keys = (e as Config).Keys;
            this.Manager = (e as Config).Manager;

            var nodes = (e as Config).Nodes;
            var nodeIds = (e as Config).NodeIds;

            this.NumOfIds = (int)Math.Pow(2, nodes.Count);

            for (var idx = 1; idx <= nodes.Count; idx++)
            {
                var start = (this.NodeId + (int)Math.Pow(2, idx - 1)) % this.NumOfIds;
                var end = (this.NodeId + (int)Math.Pow(2, idx)) % this.NumOfIds;

                var nodeId = GetSuccessorNodeId(start, nodeIds);
                this.FingerTable.Add(start, new Finger(start, end, nodes[nodeId]));
            }

            for (var idx = 0; idx < nodeIds.Count; idx++)
            {
                if (nodeIds[idx] == this.NodeId)
                {
                    this.Predecessor = nodes[WrapSubtract(idx, 1, nodeIds.Count)];
                    break;
                }
            }

            return this.RaiseEvent(new Local());
        }

        private void JoinCluster(Event e)
        {
            this.NodeId = (e as Join).Id;
            this.Manager = (e as Join).Manager;
            this.NumOfIds = (e as Join).NumOfIds;

            var nodes = (e as Join).Nodes;
            var nodeIds = (e as Join).NodeIds;

            for (var idx = 1; idx <= nodes.Count; idx++)
            {
                var start = (this.NodeId + (int)Math.Pow(2, idx - 1)) % this.NumOfIds;
                var end = (this.NodeId + (int)Math.Pow(2, idx)) % this.NumOfIds;

                var nodeId = GetSuccessorNodeId(start, nodeIds);
                this.FingerTable.Add(start, new Finger(start, end, nodes[nodeId]));
            }

            var successor = this.FingerTable[(this.NodeId + 1) % this.NumOfIds].Node;

            this.SendEvent(this.Manager, new JoinAck());
            this.SendEvent(successor, new NotifySuccessor(this.Id));
        }

        [OnEventDoAction(typeof(FindSuccessor), nameof(ProcessFindSuccessor))]
        [OnEventDoAction(typeof(FindSuccessorResp), nameof(ProcessFindSuccessorResp))]
        [OnEventDoAction(typeof(FindPredecessor), nameof(ProcessFindPredecessor))]
        [OnEventDoAction(typeof(FindPredecessorResp), nameof(ProcessFindPredecessorResp))]
        [OnEventDoAction(typeof(QueryId), nameof(ProcessQueryId))]
        [OnEventDoAction(typeof(AskForKeys), nameof(SendKeys))]
        [OnEventDoAction(typeof(AskForKeysResp), nameof(UpdateKeys))]
        [OnEventDoAction(typeof(NotifySuccessor), nameof(UpdatePredecessor))]
        [OnEventDoAction(typeof(Stabilize), nameof(ProcessStabilize))]
        [OnEventDoAction(typeof(Terminate), nameof(ProcessTerminate))]
        private class Waiting : State { }

        private void ProcessFindSuccessor(Event e)
        {
            var sender = (e as FindSuccessor).Sender;
            var key = (e as FindSuccessor).Key;

            if (this.Keys.Contains(key))
            {
                this.SendEvent(sender, new FindSuccessorResp(this.Id, key));
            }
            else if (this.FingerTable.ContainsKey(key))
            {
                this.SendEvent(sender, new FindSuccessorResp(this.FingerTable[key].Node, key));
            }
            else if (this.NodeId.Equals(key))
            {
                this.SendEvent(sender, new FindSuccessorResp(
                    this.FingerTable[(this.NodeId + 1) % this.NumOfIds].Node, key));
            }
            else
            {
                int idToAsk = -1;
                foreach (var finger in this.FingerTable)
                {
                    if (((finger.Value.Start > finger.Value.End) &&
                        (finger.Value.Start <= key || key < finger.Value.End)) ||
                        ((finger.Value.Start < finger.Value.End) &&
                        finger.Value.Start <= key && key < finger.Value.End))
                    {
                        idToAsk = finger.Key;
                    }
                }

                if (idToAsk < 0)
                {
                    idToAsk = (this.NodeId + 1) % this.NumOfIds;
                }

                if (this.FingerTable[idToAsk].Node.Equals(this.Id))
                {
                    foreach (var finger in this.FingerTable)
                    {
                        if (finger.Value.End == idToAsk ||
                            finger.Value.End == idToAsk - 1)
                        {
                            idToAsk = finger.Key;
                            break;
                        }
                    }

                    this.Assert(!this.FingerTable[idToAsk].Node.Equals(this.Id),
                        "Cannot locate successor of {0}.", key);
                }

                this.SendEvent(this.FingerTable[idToAsk].Node, new FindSuccessor(sender, key));
            }
        }

        private void ProcessFindPredecessor(Event e)
        {
            var sender = (e as FindPredecessor).Sender;
            if (this.Predecessor != null)
            {
                this.SendEvent(sender, new FindPredecessorResp(this.Predecessor));
            }
        }

        private void ProcessQueryId(Event e)
        {
            var sender = (e as QueryId).Sender;
            this.SendEvent(sender, new QueryIdResp(this.NodeId));
        }

        private void SendKeys(Event e)
        {
            var sender = (e as AskForKeys).Node;
            var senderId = (e as AskForKeys).Id;

            this.Assert(this.Predecessor.Equals(sender), "Predecessor is corrupted.");

            List<int> keysToSend = new List<int>();
            foreach (var key in this.Keys)
            {
                if (key <= senderId)
                {
                    keysToSend.Add(key);
                }
            }

            if (keysToSend.Count > 0)
            {
                foreach (var key in keysToSend)
                {
                    this.Keys.Remove(key);
                }

                this.SendEvent(sender, new AskForKeysResp(keysToSend));
            }
        }

        private void ProcessStabilize()
        {
            var successor = this.FingerTable[(this.NodeId + 1) % this.NumOfIds].Node;
            this.SendEvent(successor, new FindPredecessor(this.Id));

            foreach (var finger in this.FingerTable)
            {
                if (!finger.Value.Node.Equals(successor))
                {
                    this.SendEvent(successor, new FindSuccessor(this.Id, finger.Key));
                }
            }
        }

        private void ProcessFindSuccessorResp(Event e)
        {
            var successor = (e as FindSuccessorResp).Node;
            var key = (e as FindSuccessorResp).Key;

            this.Assert(this.FingerTable.ContainsKey(key),
                "Finger table of {0} does not contain {1}.", this.NodeId, key);
            this.FingerTable[key] = new Finger(this.FingerTable[key].Start,
                this.FingerTable[key].End, successor);
        }

        private void ProcessFindPredecessorResp(Event e)
        {
            var successor = (e as FindPredecessorResp).Node;
            if (!successor.Equals(this.Id))
            {
                this.FingerTable[(this.NodeId + 1) % this.NumOfIds] =
                    new Finger(this.FingerTable[(this.NodeId + 1) % this.NumOfIds].Start,
                    this.FingerTable[(this.NodeId + 1) % this.NumOfIds].End,
                    successor);

                this.SendEvent(successor, new NotifySuccessor(this.Id));
                this.SendEvent(successor, new AskForKeys(this.Id, this.NodeId));
            }
        }

        private void UpdatePredecessor(Event e)
        {
            var predecessor = (e as NotifySuccessor).Node;
            if (!predecessor.Equals(this.Id))
            {
                this.Predecessor = predecessor;
            }
        }

        private void UpdateKeys(Event e)
        {
            var keys = (e as AskForKeysResp).Keys;
            foreach (var key in keys)
            {
                this.Keys.Add(key);
            }
        }

        private Transition ProcessTerminate()
        {
            return this.Halt();
        }

        private static int GetSuccessorNodeId(int start, List<int> nodeIds)
        {
            var candidate = -1;
            foreach (var id in nodeIds.Where(v => v >= start))
            {
                if (candidate < 0 || id < candidate)
                {
                    candidate = id;
                }
            }

            if (candidate < 0)
            {
                foreach (var id in nodeIds.Where(v => v < start))
                {
                    if (candidate < 0 || id < candidate)
                    {
                        candidate = id;
                    }
                }
            }

            for (int idx = 0; idx < nodeIds.Count; idx++)
            {
                if (nodeIds[idx] == candidate)
                {
                    candidate = idx;
                    break;
                }
            }

            return candidate;
        }

        private int WrapAdd(int left, int right, int ceiling)
        {
            int result = left + right;
            if (result > ceiling)
            {
                result = ceiling - result;
            }

            return result;
        }

        private static int WrapSubtract(int left, int right, int ceiling)
        {
            int result = left - right;
            if (result < 0)
            {
                result = ceiling + result;
            }

            return result;
        }

        private void EmitFingerTableAndKeys()
        {
            this.Logger.WriteLine(" ... Printing finger table of node {0}:", this.NodeId);
            foreach (var finger in this.FingerTable)
            {
                this.Logger.WriteLine("  >> " + finger.Key + " | [" + finger.Value.Start +
                    ", " + finger.Value.End + ") | " + finger.Value.Node);
            }

            this.Logger.WriteLine(" ... Printing keys of node {0}:", this.NodeId);
            foreach (var key in this.Keys)
            {
                this.Logger.WriteLine("  >> Key-" + key);
            }
        }
    }
}
