// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Coyote;
using Microsoft.Coyote.Actors;

namespace Coyote.Examples.Chord
{
    internal class ClusterManager : StateMachine
    {
        #region events

        internal class CreateNewNode : Event { }

        internal class TerminateNode : Event { }

        private class Local : Event { }

        #endregion

        #region fields

        private int NumOfNodes;
        private int NumOfIds;

        private List<ActorId> ChordNodes;

        private List<int> Keys;
        private List<int> NodeIds;

        private ActorId Client;

        #endregion

        #region states

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(Local), typeof(Waiting))]
        private class Init : State { }

        private void InitOnEntry()
        {
            this.NumOfNodes = 3;
            this.NumOfIds = (int)Math.Pow(2, this.NumOfNodes);

            this.ChordNodes = new List<ActorId>();
            this.NodeIds = new List<int> { 0, 1, 3 };
            this.Keys = new List<int> { 1, 2, 6 };

            for (int idx = 0; idx < this.NodeIds.Count; idx++)
            {
                this.ChordNodes.Add(this.CreateStateMachine(typeof(ChordNode)));
            }

            var nodeKeys = this.AssignKeysToNodes();
            for (int idx = 0; idx < this.ChordNodes.Count; idx++)
            {
                var keys = nodeKeys[this.NodeIds[idx]];
                this.SendEvent(this.ChordNodes[idx], new ChordNode.Config(this.NodeIds[idx], new HashSet<int>(keys),
                    new List<ActorId>(this.ChordNodes), new List<int>(this.NodeIds), this.Id));
            }

            this.Client = this.CreateStateMachine(typeof(Client),
                new Client.Config(this.Id, new List<int>(this.Keys)));

            this.RaiseEvent(new Local());
        }

        [OnEventDoAction(typeof(ChordNode.FindSuccessor), nameof(ForwardFindSuccessor))]
        [OnEventDoAction(typeof(CreateNewNode), nameof(ProcessCreateNewNode))]
        [OnEventDoAction(typeof(TerminateNode), nameof(ProcessTerminateNode))]
        [OnEventDoAction(typeof(ChordNode.JoinAck), nameof(QueryStabilize))]
        private class Waiting : State { }

        private void ForwardFindSuccessor()
        {
            this.SendEvent(this.ChordNodes[0], this.ReceivedEvent);
        }

        private void ProcessCreateNewNode()
        {
            int newId = -1;
            while ((newId < 0 || this.NodeIds.Contains(newId)) &&
                this.NodeIds.Count < this.NumOfIds)
            {
                for (int i = 0; i < this.NumOfIds; i++)
                {
                    if (this.Random())
                    {
                        newId = i;
                    }
                }
            }

            this.Assert(newId >= 0, "Cannot create a new node, no ids available.");

            var newNode = this.CreateStateMachine(typeof(ChordNode));

            this.NumOfNodes++;
            this.NodeIds.Add(newId);
            this.ChordNodes.Add(newNode);

            this.SendEvent(newNode, new ChordNode.Join(newId, new List<ActorId>(this.ChordNodes),
                new List<int>(this.NodeIds), this.NumOfIds, this.Id));
        }

        private void ProcessTerminateNode()
        {
            int endId = -1;
            while ((endId < 0 || !this.NodeIds.Contains(endId)) &&
                this.NodeIds.Count > 0)
            {
                for (int i = 0; i < this.ChordNodes.Count; i++)
                {
                    if (this.Random())
                    {
                        endId = i;
                    }
                }
            }

            this.Assert(endId >= 0, "Cannot find a node to terminate.");

            var endNode = this.ChordNodes[endId];

            this.NumOfNodes--;
            this.NodeIds.Remove(endId);
            this.ChordNodes.Remove(endNode);

            this.SendEvent(endNode, new ChordNode.Terminate());
        }

        private void QueryStabilize()
        {
            foreach (var node in this.ChordNodes)
            {
                this.SendEvent(node, new ChordNode.Stabilize());
            }
        }

        private Dictionary<int, List<int>> AssignKeysToNodes()
        {
            var nodeKeys = new Dictionary<int, List<int>>();
            for (int i = this.Keys.Count - 1; i >= 0; i--)
            {
                bool assigned = false;
                for (int j = 0; j < this.NodeIds.Count; j++)
                {
                    if (this.Keys[i] <= this.NodeIds[j])
                    {
                        if (nodeKeys.ContainsKey(this.NodeIds[j]))
                        {
                            nodeKeys[this.NodeIds[j]].Add(this.Keys[i]);
                        }
                        else
                        {
                            nodeKeys.Add(this.NodeIds[j], new List<int>());
                            nodeKeys[this.NodeIds[j]].Add(this.Keys[i]);
                        }

                        assigned = true;
                        break;
                    }
                }

                if (!assigned)
                {
                    if (nodeKeys.ContainsKey(this.NodeIds[0]))
                    {
                        nodeKeys[this.NodeIds[0]].Add(this.Keys[i]);
                    }
                    else
                    {
                        nodeKeys.Add(this.NodeIds[0], new List<int>());
                        nodeKeys[this.NodeIds[0]].Add(this.Keys[i]);
                    }
                }
            }

            return nodeKeys;
        }

        #endregion
    }
}
