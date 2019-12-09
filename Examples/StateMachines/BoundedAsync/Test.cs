// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.Coyote;
using Microsoft.Coyote.Runtime;

namespace Coyote.Examples.BoundedAsync
{
    /// <summary>
    /// A sample application written using C# and the Coyote library.
    ///
    /// The Coyote runtime starts by creating the Coyote machine 'Scheduler'. The 'Scheduler' machine
    /// then creates a user-defined number of 'Process' machines, which communicate with each
    /// other by exchanging a 'count' value. The processes assert that their count value is
    /// always equal (or minus one) to their neighbour's count value.
    ///
    /// Note: this is an abstract implementation aimed primarily to showcase the testing
    /// capabilities of Coyote.
    /// </summary>
    public static class Program
    {
        public static void Main()
        {
            // Optional: increases verbosity level to see the Coyote runtime log.
            var configuration = Configuration.Create().WithVerbosityEnabled();

            // Creates a new Coyote runtime instance, and passes an optional configuration.
            var runtime = ActorRuntimeFactory.Create(configuration);

            // Executes the Coyote program.
            Execute(runtime);

            // The Coyote runtime executes asynchronously, so we wait
            // to not terminate the process.
            Console.WriteLine("Press Enter to terminate...");
            Console.ReadLine();
        }

        [Microsoft.Coyote.TestingServices.Test]
        public static void Execute(IActorRuntime runtime)
        {
            runtime.CreateActor(typeof(Scheduler), new Scheduler.Config(3));
        }
    }
}
