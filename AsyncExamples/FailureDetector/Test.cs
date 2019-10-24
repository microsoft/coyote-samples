// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using Microsoft.Coyote;
using Microsoft.Coyote.Specifications;
using Task = Microsoft.Coyote.Threading.Tasks.ControlledTask;

namespace Coyote.Examples.FailureDetector
{
    /// <summary>
    /// A sample application written using C# and the Coyote library.
    ///
    /// This program implements a failure detection protocol. A failure detector state
    /// machine is given a list of machines, each of which represents a daemon running
    /// at a computing node in a distributed system. The failure detector sends each
    /// machine in the list a 'Ping' event and determines whether the machine has failed
    /// if it does not respond with a 'Pong' event within a certain time period.
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
            var runtime = MachineRuntimeFactory.Create(configuration);

            // Executes the Coyote program.
            _ = Execute(runtime);

            // The Coyote runtime executes asynchronously, so we wait
            // to not terminate the process.
            Thread.Sleep(500);
            Console.WriteLine("Press Enter to terminate...");
            Console.ReadLine();
        }

        [Microsoft.Coyote.Test]
        public static async Task Execute(IMachineRuntime runtime)
        {
            // Monitors must be registered before the first Coyote machine
            // gets created (which will kickstart the runtime).
            Specification.RegisterMonitor<Safety>();
            Specification.RegisterMonitor<Liveness>();
            Driver driver = new Driver(2, runtime.Logger);
            await driver.Run();
        }
    }
}
