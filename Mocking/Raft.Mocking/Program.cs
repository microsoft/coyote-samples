// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.TestingServices;
using Microsoft.Coyote.Threading.Tasks;

namespace Microsoft.Coyote.Samples.Mocking
{
    public static class Program
    {
        [Test]
        public static async ControlledTask Execute(IActorRuntime runtime)
        {
            var testScenario = new RaftTestScenario();
            await testScenario.RunTestAsync(runtime, 5, 2);
        }
    }
}
