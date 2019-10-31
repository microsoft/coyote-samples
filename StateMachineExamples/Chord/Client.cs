// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Coyote;
using Microsoft.Coyote.Actors;

namespace Coyote.Examples.Chord
{
    internal class Client : StateMachine
    {
        #region events

        internal class Config : Event
        {
            public ActorId ClusterManager;
            public List<int> Keys;

            public Config(ActorId clusterManager, List<int> keys)
                : base()
            {
                this.ClusterManager = clusterManager;
                this.Keys = keys;
            }
        }

        private class Local : Event { }

        #endregion

        #region fields

        private ActorId ClusterManager;

        private List<int> Keys;
        private int QueryCounter;

        #endregion

        #region states

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(Local), typeof(Querying))]
        private class Init : State { }

        private void InitOnEntry()
        {
            this.ClusterManager = (this.ReceivedEvent as Config).ClusterManager;
            this.Keys = (this.ReceivedEvent as Config).Keys;

            // LIVENESS BUG: can never detect the key, and keeps looping without
            // exiting the process. Enable to introduce the bug.
            this.Keys.Add(17);

            this.QueryCounter = 0;

            this.RaiseEvent(new Local());
        }

        [OnEntry(nameof(QueryingOnEntry))]
        [OnEventGotoState(typeof(Local), typeof(Waiting))]
        private class Querying : State { }

        private void QueryingOnEntry()
        {
            if (this.QueryCounter < 5)
            {
                if (this.Random())
                {
                    var key = this.GetNextQueryKey();
                    this.Logger.WriteLine($"<ChordLog> Client is searching for successor of key '{key}'.");
                    this.SendEvent(this.ClusterManager, new ChordNode.FindSuccessor(this.Id, key));
                    this.Monitor<LivenessMonitor>(new LivenessMonitor.NotifyClientRequest(key));
                }
                else if (this.Random())
                {
                    this.SendEvent(this.ClusterManager, new ClusterManager.CreateNewNode());
                }
                else
                {
                    this.SendEvent(this.ClusterManager, new ClusterManager.TerminateNode());
                }

                this.QueryCounter++;
            }

            this.RaiseEvent(new Local());
        }

        private int GetNextQueryKey()
        {
            int keyIndex = -1;
            while (keyIndex < 0)
            {
                for (int i = 0; i < this.Keys.Count; i++)
                {
                    if (this.Random())
                    {
                        keyIndex = i;
                        break;
                    }
                }
            }

            return this.Keys[keyIndex];
        }

        [OnEventGotoState(typeof(Local), typeof(Querying))]
        [OnEventDoAction(typeof(ChordNode.FindSuccessorResp), nameof(ProcessFindSuccessorResp))]
        [OnEventDoAction(typeof(ChordNode.QueryIdResp), nameof(ProcessQueryIdResp))]
        private class Waiting : State { }

        private void ProcessFindSuccessorResp()
        {
            var successor = (this.ReceivedEvent as ChordNode.FindSuccessorResp).Node;
            var key = (this.ReceivedEvent as ChordNode.FindSuccessorResp).Key;
            this.Monitor<LivenessMonitor>(new LivenessMonitor.NotifyClientResponse(key));
            this.SendEvent(successor, new ChordNode.QueryId(this.Id));
        }

        private void ProcessQueryIdResp()
        {
            this.RaiseEvent(new Local());
        }

        #endregion
    }
}
