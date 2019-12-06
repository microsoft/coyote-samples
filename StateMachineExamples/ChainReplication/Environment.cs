// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Coyote.Actors;

namespace Coyote.Examples.ChainReplication
{
    internal class Environment : StateMachine
    {
        private List<ActorId> Servers;
        private List<ActorId> Clients;

        private int NumOfServers;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        private class Init : State { }

        private Transition InitOnEntry()
        {
            this.Servers = new List<ActorId>();
            this.Clients = new List<ActorId>();

            this.NumOfServers = 3;

            for (int i = 0; i < this.NumOfServers; i++)
            {
                ActorId server;

                if (i == 0)
                {
                    server = this.CreateActor(typeof(ChainReplicationServer),
                        new ChainReplicationServer.Config(i, true, false));
                }
                else if (i == this.NumOfServers - 1)
                {
                    server = this.CreateActor(typeof(ChainReplicationServer),
                        new ChainReplicationServer.Config(i, false, true));
                }
                else
                {
                    server = this.CreateActor(typeof(ChainReplicationServer),
                        new ChainReplicationServer.Config(i, false, false));
                }

                this.Servers.Add(server);
            }

            this.Monitor<InvariantMonitor>(
                new InvariantMonitor.Config(this.Servers));
            this.Monitor<ServerResponseSeqMonitor>(
                new ServerResponseSeqMonitor.Config(this.Servers));

            for (int i = 0; i < this.NumOfServers; i++)
            {
                ActorId pred;
                ActorId succ;

                if (i > 0)
                {
                    pred = this.Servers[i - 1];
                }
                else
                {
                    pred = this.Servers[0];
                }

                if (i < this.NumOfServers - 1)
                {
                    succ = this.Servers[i + 1];
                }
                else
                {
                    succ = this.Servers[this.NumOfServers - 1];
                }

                this.SendEvent(this.Servers[i], new ChainReplicationServer.PredSucc(pred, succ));
            }

            this.Clients.Add(this.CreateActor(typeof(Client),
                new Client.Config(0, this.Servers[0], this.Servers[this.NumOfServers - 1], 1)));

            this.Clients.Add(this.CreateActor(typeof(Client),
                new Client.Config(1, this.Servers[0], this.Servers[this.NumOfServers - 1], 100)));

            this.CreateActor(typeof(ChainReplicationMaster),
                new ChainReplicationMaster.Config(this.Servers, this.Clients));

            return this.Halt();
        }
    }
}
