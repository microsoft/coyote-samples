// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace Microsoft.Coyote.Samples.HelloWorldTasks
{
    public static class Program
    {
        public static async Task Main()
        {
            await Execute();
        }

        [Microsoft.Coyote.SystematicTesting.Test]
        public static async Task Execute()
        {
            var greeter = new Greeter();
            await greeter.RunAsync();
        }
    }
}
