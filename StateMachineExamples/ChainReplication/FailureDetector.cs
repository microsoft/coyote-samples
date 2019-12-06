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

        private ActorId Master;
        private List<ActorId> Servers;

        private int CheckNodeIdx;
        private int Failures;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(Local), typeof(StartMonitoring))]
        private class Init : State { }

        private Transition InitOnEntry(Event e)
        {
            this.Master = (e as Config).Master;
            this.Servers = (e as Config).Servers;
            this.CheckNodeIdx = 0;
            this.Failures = 100;
            return this.RaiseEvent(new Local());
        }

        [OnEntry(nameof(StartMonitoringOnEntry))]
        [OnEventGotoState(typeof(Pong), typeof(StartMonitoring), nameof(HandlePong))]
        [OnEventGotoState(typeof(InjectFailure), typeof(HandleFailure))]
        private class StartMonitoring : State { }

        private Transition StartMonitoringOnEntry()
        {
            if (this.Failures < 1)
            {
                return this.Halt();
            }

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
            return default;
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

        private void ProcessFailureCorrected(Event e)
        {
            this.CheckNodeIdx = 0;
            this.Servers = (e as FailureCorrected).Servers;
        }
    }
}
