// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Coyote;
using Microsoft.Coyote.Machines;

namespace Coyote.Examples.ChainReplication
{
    internal class FailureDetector : Machine
    {
        #region events

        internal class Config : Event
        {
            public MachineId Master;
            public List<MachineId> Servers;

            public Config(MachineId master, List<MachineId> servers)
                : base()
            {
                this.Master = master;
                this.Servers = servers;
            }
        }

        internal class FailureDetected : Event
        {
            public MachineId Server;

            public FailureDetected(MachineId server)
                : base()
            {
                this.Server = server;
            }
        }

        internal class FailureCorrected : Event
        {
            public List<MachineId> Servers;

            public FailureCorrected(List<MachineId> servers)
                : base()
            {
                this.Servers = servers;
            }
        }

        internal class Ping : Event
        {
            public MachineId Target;

            public Ping(MachineId target)
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

        private MachineId Master;
        private List<MachineId> Servers;

        private int CheckNodeIdx;
        private int Failures;

        #endregion

        #region states

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(Local), typeof(StartMonitoring))]
        private class Init : MachineState { }

        private void InitOnEntry()
        {
            this.Master = (this.ReceivedEvent as Config).Master;
            this.Servers = (this.ReceivedEvent as Config).Servers;

            this.CheckNodeIdx = 0;
            this.Failures = 100;

            this.Raise(new Local());
        }

        [OnEntry(nameof(StartMonitoringOnEntry))]
        [OnEventGotoState(typeof(Pong), typeof(StartMonitoring), nameof(HandlePong))]
        [OnEventGotoState(typeof(InjectFailure), typeof(HandleFailure))]
        private class StartMonitoring : MachineState { }

        private void StartMonitoringOnEntry()
        {
            if (this.Failures < 1)
            {
                this.Raise(new Halt());
            }
            else
            {
                this.Send(this.Servers[this.CheckNodeIdx], new Ping(this.Id));

                if (this.Servers.Count > 1)
                {
                    if (this.Random())
                    {
                        this.Send(this.Id, new InjectFailure());
                    }
                    else
                    {
                        this.Send(this.Id, new Pong());
                    }
                }
                else
                {
                    this.Send(this.Id, new Pong());
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
        private class HandleFailure : MachineState { }

        private void HandleFailureOnEntry()
        {
            this.Send(this.Master, new FailureDetected(this.Servers[this.CheckNodeIdx]));
        }

        private void ProcessFailureCorrected()
        {
            this.CheckNodeIdx = 0;
            this.Servers = (this.ReceivedEvent as FailureCorrected).Servers;
        }

        #endregion
    }
}
