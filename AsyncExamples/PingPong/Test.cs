// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.Coyote.Threading.Tasks;

namespace Coyote.Examples.PingPong
{
    /// <summary>
    /// A simple PingPong application written using C# and the Coyote library.
    ///
    /// The Coyote runtime starts by creating the Coyote machine 'NetworkEnvironment'. The
    /// 'NetworkEnvironment' machine then creates a 'Server' and a 'Client' machine,
    /// which then communicate by sending 'Ping' and 'Pong' events to each other for
    /// a limited amount of turns.
    ///
    /// Note: this is an abstract implementation aimed primarily to showcase the testing
    /// capabilities of Coyote.
    /// </summary>
    public static class Program
    {
        public static void Main()
        {
            // Executes the sample Coyote program.
            Execute().GetAwaiter().GetResult();

            Console.WriteLine("Press Enter to terminate...");
            Console.ReadLine();
        }

        /// <summary>
        /// The Coyote testing engine uses a method annotated with the 'Microsoft.Coyote.Test'
        /// attribute as an entry point.
        ///
        /// During testing, the testing engine takes control of the underlying scheduler
        /// and any declared in Coyote sources of non-determinism (e.g. Coyote asynchronous APIs,
        /// Coyote non-determinstic choices) and systematically executes the test method a user
        /// specified number of iterations to detect bugs.
        /// </summary>
        [Microsoft.Coyote.Test]
        public static async ControlledTask Execute()
        {
            // This is the root machine to the PingPong program. CreateMachine
            // executes asynchronously (i.e. non-blocking).
            var environment = new NetworkEnvironment();
            await environment.Run();
        }
    }
}
