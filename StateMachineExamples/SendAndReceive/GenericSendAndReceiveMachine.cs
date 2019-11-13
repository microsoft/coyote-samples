// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Coyote;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;

namespace Coyote.Examples.SendAndReceive
{
    /// <summary>
    /// Generic machine that helps fetch response.
    /// </summary>
    internal class GetReponseMachine<T> : StateMachine
        where T : Event
    {
        /// <summary>
        /// Static method for safely getting a response from a machine.
        /// </summary>
        /// <param name="runtime">The runtime.</param>
        /// <param name="mid">Target machine id.</param>
        /// <param name="ev">Event to send whose respose we're interested in getting.</param>
        public static async Task<T> GetResponse(IActorRuntime runtime, ActorId mid, Func<ActorId, Event> ev)
        {
            var conf = new Config(mid, ev);
            // This method awaits until the GetResponseMachine finishes its Execute method
            await runtime.CreateActorAndExecuteAsync(typeof(GetReponseMachine<T>), conf);
            // Safely return the result back (no race condition here)
            return conf.ReceivedEvent;
        }

        /// <summary>
        /// Internal config event.
        /// </summary>
        private class Config : Event
        {
            public ActorId TargetMachineId;
            public Func<ActorId, Event> Ev;
            public T ReceivedEvent;

            public Config(ActorId targetMachineId, Func<ActorId, Event> ev)
            {
                this.TargetMachineId = targetMachineId;
                this.Ev = ev;
                this.ReceivedEvent = null;
            }
        }

        [Start]
        [OnEntry(nameof(Execute))]
        private class Init : State { }

        private async Task Execute()
        {
            // Grab the config event.
            var config = this.ReceivedEvent as Config;
            // send event to target machine, adding self Id
            this.SendEvent(config.TargetMachineId, config.Ev(this.Id));
            // Wait for the response.
            var rv = await this.ReceiveEventAsync(typeof(T));
            // Stash in the shared config event.
            config.ReceivedEvent = rv as T;
            // Finally, halt.
            this.RaiseEvent(new HaltEvent());
        }
    }
}
