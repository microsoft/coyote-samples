// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Samples.HelloWorld;
using Microsoft.Coyote.Threading.Tasks;

namespace Microsoft.Coyote.Samples.HelloWorldTasks
{
    public static class Program
    {
        public static async Task Main()
        {
            await Execute();
        }

        [Microsoft.Coyote.TestingServices.Test]
        public static async ControlledTask Execute()
        {
            var greeter = new Greeter();
            await greeter.RunAsync();
        }
    }
}
