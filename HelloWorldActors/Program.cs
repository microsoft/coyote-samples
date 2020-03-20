// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;

namespace Microsoft.Coyote.Samples.HelloWorld
{
    public static class HostProgram
    {
        private static long MaxGreetings = 7;

        private static readonly TaskCompletionSource<bool> CompletionSource = new TaskCompletionSource<bool>();

        public static async Task Main(string[] args)
        {
            MaxGreetings = (args != null && args.Length > 0 && long.TryParse(args[0], out MaxGreetings))
                ? MaxGreetings
                : 7;

            IActorRuntime runtime = RuntimeFactory.Create();
            Execute(runtime);

            await CompletionSource.Task;
        }

        [Microsoft.Coyote.SystematicTesting.Test]
        public static void Execute(IActorRuntime runtime)
        {
            ActorId serverId = runtime.CreateActor(typeof(Server));
            runtime.CreateActor(typeof(Client), new Client.ConfigEvent(CompletionSource, serverId, MaxGreetings));

            return;
        }
    }
}
