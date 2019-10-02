// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;

using Microsoft.Coyote;
using Microsoft.Coyote.Machines;

namespace Coyote.Examples.SendAndReceive
{
    public static class Program
    {
        public static void Main()
        {
            var runtime = MachineRuntimeFactory.Create();

            // Create a machine.
            var mid = runtime.CreateMachine(typeof(M1));

            // Do some work.
            runtime.SendEvent(mid, new M1.Inc());
            runtime.SendEvent(mid, new M1.Inc());
            runtime.SendEvent(mid, new M1.Inc());

            // Grab the result from the machine.
            GetDataAndPrint(runtime, mid).Wait();
        }

        /// <summary>
        /// Gets result from the given machine.
        /// </summary>
        /// <param name="runtime">The Coyote runtime.</param>
        /// <param name="mid">Machine to get response from.</param>
        private static async Task GetDataAndPrint(IMachineRuntime runtime, MachineId mid)
        {
            var resp = await GetReponseMachine<M1.Response>.GetResponse(runtime, mid, m => new M1.Get(m));
            Console.WriteLine("Got response: {0}", resp.V);
        }
    }

    /// <summary>
    /// A simple machine.
    /// </summary>
    internal class M1 : Machine
    {
        public class Get : Event
        {
            public MachineId Mid;

            public Get(MachineId mid)
            {
                this.Mid = mid;
            }
        }

        public class Inc : Event { }

        public class Response : Event
        {
            public int V;

            public Response(int v)
            {
                this.V = v;
            }
        }

        /// <summary>
        /// The counter.
        /// </summary>
        private int X;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventDoAction(typeof(Inc), nameof(DoInc))]
        [OnEventDoAction(typeof(Get), nameof(DoGet))]
        private class Init : MachineState { }

        private void InitOnEntry()
        {
            this.X = 0;
        }

        private void DoInc()
        {
            this.X++;
        }

        /// <summary>
        /// Sends the current value of the counter.
        /// </summary>
        private void DoGet()
        {
            var sender = (this.ReceivedEvent as Get).Mid;
            this.Send(sender, new Response(this.X));
        }
    }
}
