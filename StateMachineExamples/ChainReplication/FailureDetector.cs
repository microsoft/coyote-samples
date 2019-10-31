// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Coyote;
using Microsoft.Coyote.Actors;

namespace Coyote.Examples.ChainReplication
{
    internal class FailureDetector : StateMachine
    {
        #region events

        internal class Config : Event
        {
            public ActorId Master;
            public List<ActorId> Servers;

            public Config(ActorId master, List<ActorId> servers)
                : base()
            {
                this.Master = master;
                this.Servers = servers;
            }
        }

        internal class FailureDetected : Event
        {
            public ActorId Server;

            public FailureDetected(ActorId server)
                : base()
            {
                this.Server = server;
            }
        }

        internal class FailureCorrected : Event
        {
            public List<ActorId> Servers;

            public FailureCorrected(List<ActorId> servers)
                : base()
            {
                this.Servers = servers;
            }
        }

        internal class Ping : Event
        {
            public ActorId Target;

            public Ping(ActorId target)
                : base()
            {
                this.Target = target;
            }
        }

        internal class Pong : Event { }

        private class InjectFailure : Event { }

        private class Local : Event { }

        #endregion

        #region fields

        private ActorId Master;
        private List<ActorId> Servers;

        private int CheckNodeIdx;
        private int Failures;

        #endregion

        #region states

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(Local), typeof(StartMonitoring))]
        private class Init : State { }

        private void InitOnEntry()
        {
            this.Master = (this.ReceivedEvent as Config).Master;
            this.Servers = (this.ReceivedEvent as Config).Servers;

            this.CheckNodeIdx = 0;
            this.Failures = 100;

            this.RaiseEvent(new Local());
        }

        [OnEntry(nameof(StartMonitoringOnEntry))]
        [OnEventGotoState(typeof(Pong), typeof(StartMonitoring), nameof(HandlePong))]
        [OnEventGotoState(typeof(InjectFailure), typeof(HandleFailure))]
        private class StartMonitoring : State { }

        private void StartMonitoringOnEntry()
        {
            if (this.Failures < 1)
            {
                this.RaiseEvent(new Halt());
            }
            else
            {
                this.SendEvent(this.Servers[this.CheckNodeIdx], new Ping(this.Id));

                if (this.Servers.Count > 1)
                {
                    if (this.Random())
                    {
                        this.SendEvent(this.Id, new InjectFailure());
                    }
                    else
                    {
                        this.SendEvent(this.Id, new Pong());
                    }
                }
                else
                {
                    this.SendEvent(this.Id, new Pong());
                }

                this.Failures--;
            }
        }

        private void HandlePong()
        {
            this.CheckNodeIdx++;
            if (this.CheckNodeIdx == this.Servers.Count)
            {
                this.CheckNodeIdx = 0;
            }
        }

        [OnEntry(nameof(HandleFailureOnEntry))]
        [OnEventGotoState(typeof(FailureCorrected), typeof(StartMonitoring), nameof(ProcessFailureCorrected))]
        [IgnoreEvents(typeof(Pong), typeof(InjectFailure))]
        private class HandleFailure : State { }

        private void HandleFailureOnEntry()
        {
            this.SendEvent(this.Master, new FailureDetected(this.Servers[this.CheckNodeIdx]));
        }

        private void ProcessFailureCorrected()
        {
            this.CheckNodeIdx = 0;
            this.Servers = (this.ReceivedEvent as FailureCorrected).Servers;
        }

        #endregion
    }
}
