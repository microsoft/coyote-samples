// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote;
using Microsoft.Coyote.Runtime;

namespace Coyote.Examples.Timers
{
    public static class Program
    {
        public static void Main()
        {
            // Optional: increases verbosity level to see the Coyote runtime log.
            // var configuration = Configuration.Create();
            var configuration = Configuration.Create().WithVerbosityEnabled();

            // Creates a new Coyote runtime instance, and passes an optional configuration.
            var runtime = ActorRuntimeFactory.Create(configuration);

            // Executes the Coyote program.
            Execute(runtime);

            // The Coyote runtime executes asynchronously, so we wait
            // here until user presses ENTER key before terminating the program.
            Console.ReadLine();
        }

        [Microsoft.Coyote.TestingServices.Test]
        public static void Execute(IActorRuntime runtime)
        {
            runtime.CreateActor(typeof(TimerSample));
        }
    }
}
