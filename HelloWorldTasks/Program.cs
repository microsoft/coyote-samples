// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Samples.HelloWorld;
using Microsoft.Coyote.Tasks;

namespace Microsoft.Coyote.Samples.HelloWorldTasks
{
    public static class Program
    {
        public static async System.Threading.Tasks.Task Main()
        {
            await Execute();
        }

        [Microsoft.Coyote.SystematicTesting.Test]
        public static async Tasks.Task Execute()
        {
            var greeter = new Greeter();
            await greeter.RunAsync();
        }
    }
}
