// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tasks;

namespace Microsoft.Coyote.Samples.CoffeeMachineTasks
{
    public static class Program
    {
        private static bool RunForever = false;

        public static async Task Main()
        {
            RunForever = true;
            // bugbug: this is weird that I have to create an IActorRuntime
            // in order to use the runtime.Logger.  We need a better way for
            // Task based apps.
            IActorRuntime runtime = ActorRuntimeFactory.Create();
            await Execute(runtime);
            Console.ReadLine();
            Console.WriteLine("User cancelled the test by pressing ENTER");
        }

        private static void OnRuntimeFailure(Exception ex)
        {
            Console.WriteLine("### Failure: " + ex.Message);
        }

        [Microsoft.Coyote.TestingServices.Test]
        public static async ControlledTask Execute(IActorRuntime runtime)
        {
            runtime.OnFailure += OnRuntimeFailure;
            Specification.RegisterMonitor<LivenessMonitor>();
            IFailoverDriver driver = new FailoverDriver(RunForever, runtime.Logger);
            await driver.RunTest();
        }
    }

    internal class Loggable
    {
        protected readonly TextWriter Log;
        private readonly bool Echo = false;

        public Loggable(TextWriter writer, bool echo)
        {
            this.Log = writer;
            this.Echo = echo;
        }

        internal void WriteLine(string format, params object[] args)
        {
            var msg = string.Format(format, args);
            msg = string.Format("<{0}> {1}", this.GetType().Name, msg);
            this.Log.WriteLine(msg);
            if (this.Echo)
            {
                Console.WriteLine(msg);
            }
        }
    }
}
