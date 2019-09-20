// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Coyote.Machines;

namespace Coyote.Examples.ChainReplication
{
    internal class ChainReplicationMaster : Machine
    {
        #region events

        internal class Config : Event
        {
            public List<MachineId> Servers;
            public List<MachineId> Clients;

            public Config(List<MachineId> servers, List<MachineId> clients)
                : base()
            {
                this.Servers = servers;
                this.Clients = clients;
            }
        }

        internal class BecomeHead : Event
        {
            public MachineId Target;

            public BecomeHead(MachineId target)
                : base()
            {
                this.Target = target;
            }
        }

        internal class BecomeTail : Event
        {
            public MachineId Target;

            public BecomeTail(MachineId target)
                : base()
            {
                this.Target = target;
            }
        }

        internal class Success : Event { }

        internal class HeadChanged : Event { }

        internal class TailChanged : Event { }

        private class HeadFailed : Event { }

        private class TailFailed : Event { }

        private class ServerFailed : Event { }

        private class FixSuccessor : Event { }

        private class FixPredecessor : Event { }

        private class Local : Event { }

        private class Done : Event { }

        #endregion

        #region fields

        private List<MachineId> Servers;
        private List<MachineId> Clients;

        private MachineId FailureDetector;

        private MachineId Head;
        private MachineId Tail;

        private int FaultyNodeIndex;
        private int LastUpdateReceivedSucc;
        private int LastAckSent;

        #endregion

        #region states

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(Local), typeof(WaitForFailure))]
        private class Init : MachineState { }

        private void InitOnEntry()
        {
            this.Servers = (this.ReceivedEvent as Config).Servers;
            this.Clients = (this.ReceivedEvent as Config).Clients;

            this.FailureDetector = this.CreateMachine(typeof(FailureDetector),
                new FailureDetector.Config(this.Id, this.Servers));

            this.Head = this.Servers[0];
            this.Tail = this.Servers[this.Servers.Count - 1];

            this.Raise(new Local());
        }

        [OnEventGotoState(typeof(HeadFailed), typeof(CorrectHeadFailure))]
        [OnEventGotoState(typeof(TailFailed), typeof(CorrectTailFailure))]
        [OnEventGotoState(typeof(ServerFailed), typeof(CorrectServerFailure))]
        [OnEventDoAction(typeof(FailureDetector.FailureDetected), nameof(CheckWhichNodeFailed))]
        private class WaitForFailure : MachineState { }

        private void CheckWhichNodeFailed()
        {
            this.Assert(this.Servers.Count > 1, "All nodes have failed.");

            var failedServer = (this.ReceivedEvent as FailureDetector.FailureDetected).Server;

            if (this.Head.Equals(failedServer))
            {
                this.Raise(new HeadFailed());
            }
            else if (this.Tail.Equals(failedServer))
            {
                this.Raise(new TailFailed());
            }
            else
            {
                for (int i = 0; i < this.Servers.Count - 1; i++)
                {
                    if (this.Servers[i].Equals(failedServer))
                    {
                        this.FaultyNodeIndex = i;
                    }
                }

                this.Raise(new ServerFailed());
            }
        }

        [OnEntry(nameof(CorrectHeadFailureOnEntry))]
        [OnEventGotoState(typeof(Done), typeof(WaitForFailure), nameof(UpdateFailureDetector))]
        [OnEventDoAction(typeof(HeadChanged), nameof(UpdateClients))]
        private class CorrectHeadFailure : MachineState { }

        private void CorrectHeadFailureOnEntry()
        {
            this.Servers.RemoveAt(0);

            this.Monitor<InvariantMonitor>(
                new InvariantMonitor.UpdateServers(this.Servers));
            this.Monitor<ServerResponseSeqMonitor>(
                new ServerResponseSeqMonitor.UpdateServers(this.Servers));

            this.Head = this.Servers[0];

            this.Send(this.Head, new BecomeHead(this.Id));
        }

        private void UpdateClients()
        {
            for (int i = 0; i < this.Clients.Count; i++)
            {
                this.Send(this.Clients[i], new Client.UpdateHeadTail(this.Head, this.Tail));
            }

            this.Raise(new Done());
        }

        private void UpdateFailureDetector()
        {
            this.Send(this.FailureDetector, new FailureDetector.FailureCorrected(this.Servers));
        }

        [OnEntry(nameof(CorrectTailFailureOnEntry))]
        [OnEventGotoState(typeof(Done), typeof(WaitForFailure), nameof(UpdateFailureDetector))]
        [OnEventDoAction(typeof(TailChanged), nameof(UpdateClients))]
        private class CorrectTailFailure : MachineState { }

        private void CorrectTailFailureOnEntry()
        {
            this.Servers.RemoveAt(this.Servers.Count - 1);

            this.Monitor<InvariantMonitor>(
                new InvariantMonitor.UpdateServers(this.Servers));
            this.Monitor<ServerResponseSeqMonitor>(
                new ServerResponseSeqMonitor.UpdateServers(this.Servers));

            this.Tail = this.Servers[this.Servers.Count - 1];

            this.Send(this.Tail, new BecomeTail(this.Id));
        }

        [OnEntry(nameof(CorrectServerFailureOnEntry))]
        [OnEventGotoState(typeof(Done), typeof(WaitForFailure), nameof(UpdateFailureDetector))]
        [OnEventDoAction(typeof(FixSuccessor), nameof(UpdateClients))]
        [OnEventDoAction(typeof(FixPredecessor), nameof(ProcessFixPredecessor))]
        [OnEventDoAction(typeof(ChainReplicationServer.NewSuccInfo), nameof(SetLastUpdate))]
        [OnEventDoAction(typeof(Success), nameof(ProcessSuccess))]
        private class CorrectServerFailure : MachineState { }

        private void CorrectServerFailureOnEntry()
        {
            this.Servers.RemoveAt(this.FaultyNodeIndex);

            this.Monitor<InvariantMonitor>(
                new InvariantMonitor.UpdateServers(this.Servers));
            this.Monitor<ServerResponseSeqMonitor>(
                new ServerResponseSeqMonitor.UpdateServers(this.Servers));

            this.Raise(new FixSuccessor());
        }

        private void ProcessFixSuccessor()
        {
            this.Send(this.Servers[this.FaultyNodeIndex], new ChainReplicationServer.NewPredecessor(
                this.Id, this.Servers[this.FaultyNodeIndex - 1]));
        }

        private void ProcessFixPredecessor()
        {
            this.Send(this.Servers[this.FaultyNodeIndex - 1], new ChainReplicationServer.NewSuccessor(this.Id,
                this.Servers[this.FaultyNodeIndex], this.LastAckSent, this.LastUpdateReceivedSucc));
        }

        private void SetLastUpdate()
        {
            this.LastUpdateReceivedSucc = (this.ReceivedEvent as
                ChainReplicationServer.NewSuccInfo).LastUpdateReceivedSucc;
            this.LastAckSent = (this.ReceivedEvent as
                ChainReplicationServer.NewSuccInfo).LastAckSent;
            this.Raise(new FixPredecessor());
        }

        private void ProcessSuccess()
        {
            this.Raise(new Done());
        }

        #endregion
    }
}
