// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Samples.DrinksServingRobot
{
    public static class Program
    {
        private static bool RunForever = false;

        public static void Main()
        {
            var conf = null as Configuration;
            // var conf = Configuration.Create().WithVerbosityEnabled();

            RunForever = true;
            IActorRuntime runtime = ActorRuntimeFactory.Create(conf);
            runtime.OnFailure += OnRuntimeFailure;
            Execute(runtime);
            Console.ReadLine();
        }

        [Microsoft.Coyote.TestingServices.Test]
        public static void Execute(IActorRuntime runtime)
        {
            runtime.RegisterMonitor(typeof(LivenessMonitor));
            ActorId driver = runtime.CreateActor(typeof(FailoverDriver), new FailoverDriver.ConfigEvent(RunForever));
        }

        private static void OnRuntimeFailure(Exception ex)
        {
            Console.WriteLine("### Error: {0}", ex.Message);
        }
    }
}
