// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.TestingServices;

namespace Microsoft.Coyote.Samples.CoffeeMachine
{
    public static class Program
    {
        private static bool RunForever = false;

        public static void Main()
        {
            RunForever = true;
            IActorRuntime runtime = ActorRuntimeFactory.Create(); // Configuration.Create().WithVerbosityEnabled());
            Execute(runtime);
            Console.ReadLine();
            Console.WriteLine("User cancelled the test by pressing ENTER");
        }

        [Microsoft.Coyote.TestingServices.Test]
        public static void Execute(IActorRuntime runtime)
        {
            runtime.RegisterMonitor(typeof(LivenessMonitor));
            ActorId driver = runtime.CreateActor(typeof(FailoverDriver), new FailoverDriver.ConfigEvent(RunForever));
            runtime.SendEvent(driver, new FailoverDriver.StartTestEvent());
        }
    }
}
