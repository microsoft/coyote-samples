// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.TestingServices;

namespace Microsoft.Coyote.Samples.CloudMessaging
{
    public static class Program
    {
        [Test]
        public static void Execute(IActorRuntime runtime)
        {
            var testScenario = new RaftTestScenarioWithFailure();
            testScenario.RunTest(runtime, 5, 2);
        }
    }
}
