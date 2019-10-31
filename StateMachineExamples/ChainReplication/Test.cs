// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.Coyote;
using Microsoft.Coyote.Runtime;

namespace Coyote.Examples.ChainReplication
{
    /// <summary>
    /// A single-process implementation of the chain replication protocol written
    /// using C# and the Coyote library.
    ///
    /// The chain replication protocol is described in the following paper:
    /// http://www.cs.cornell.edu/home/rvr/papers/OSDI04.pdf
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

        [Microsoft.Coyote.Test]
        public static void Execute(IActorRuntime runtime)
        {
            runtime.RegisterMonitor(typeof(InvariantMonitor));
            runtime.RegisterMonitor(typeof(ServerResponseSeqMonitor));
            runtime.CreateStateMachine(typeof(Environment));
        }
    }
}
