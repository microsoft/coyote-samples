// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Samples.CloudMessaging
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            string connectionString = string.Empty;
            string topicName = string.Empty;
            int numServers = -1;
            int serverId = -1;
            int clientProcessId = -1;

            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--connection-string":
                        connectionString = args[i + 1];
                        break;

                    case "--topic-name":
                        topicName = args[i + 1];
                        break;

                    case "--num-servers":
                        if (int.TryParse(args[i + 1], out int numServersValue))
                        {
                            numServers = numServersValue;
                        }

                        break;

                    case "--server-id":
                        if (int.TryParse(args[i + 1], out int serverIdValue))
                        {
                            serverId = serverIdValue;
                        }

                        break;

                    case "--client-process-id":
                        if (int.TryParse(args[i + 1], out int clientProcessIdValue))
                        {
                            clientProcessId = clientProcessIdValue;
                        }

                        break;
                }
            }

            if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(topicName) ||
                numServers < 0 || serverId < 0)
            {
                Console.WriteLine("Error: please specify the --connection-string, --topic-name, " +
                    "--num-servers and --server-id arguments.");
                return;
            }

            if (clientProcessId > 0)
            {
                MonitorClientProcess(clientProcessId);
            }

            // We create a new Coyote actor runtime instance, and pass an optional configuration
            // that increases the verbosity level to see the Coyote runtime log.
            IActorRuntime runtime = ActorRuntimeFactory.Create(Configuration.Create().WithVerbosityEnabled());
            runtime.OnFailure += RuntimeOnFailure;

            // We create a server host that will create and wrap a Raft server instance (implemented
            // as a Coyote state machine), and execute it using the Coyote runtime.
            IServerHost host = new ServerHost(runtime, connectionString, topicName, serverId, numServers);
            await host.RunAsync(CancellationToken.None);
        }

        /// <summary>
        /// Callback that is invoked when an unhandled exception is thrown in the Coyote runtime.
        /// </summary>
        private static void RuntimeOnFailure(Exception ex)
        {
            int processId = Process.GetCurrentProcess().Id;
            Console.WriteLine($"Server process with id {processId} failed with exception:");
            Console.WriteLine(ex);
            Environment.Exit(1);
        }

        #region infrastructure code
        private static void MonitorClientProcess(int clientProcessId)
        {
            Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        Process.GetProcessById(clientProcessId);
                        await Task.Delay(2000);
                    }
                }
                catch (Exception ex)
                {
                    if (ex is ArgumentException argEx)
                    {
                        Console.WriteLine($"Client process with id {clientProcessId} " +
                            "is not running. Terminating server ...");
                        Environment.Exit(0);
                    }

                    Console.WriteLine($"{DateTime.Now} :: ex: {ex.Message}");
                }
            });
        }
        #endregion
    }
}
