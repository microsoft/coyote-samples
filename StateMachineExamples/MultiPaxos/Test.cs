// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.Coyote;
using Microsoft.Coyote.Runtime;

namespace Coyote.Examples.MultiPaxos
{
    /// <summary>
    /// A single-process implementation of the MultiPaxos consensus protocol written using
    /// C# and the Coyote library.
    ///
    /// A brief description of the MultiPaxos protocol can be found here:
    /// http://amberonrails.com/paxosmulti-paxos-algorithm/
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
            runtime.RegisterMonitor(typeof(ValidityCheck));
            runtime.CreateStateMachine(typeof(GodMachine));
        }
    }
}
