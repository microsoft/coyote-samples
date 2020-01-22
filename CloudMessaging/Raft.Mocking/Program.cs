// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.TestingServices;
using Microsoft.Coyote.Threading.Tasks;

namespace Microsoft.Coyote.Samples.CloudMessaging
{
    public static class Program
    {
        [Test]
        public static void Execute(IActorRuntime runtime)
        {
            var testScenario = new RaftTestScenario();
            testScenario.RunTest(runtime, 5, 2);
        }
    }
}
