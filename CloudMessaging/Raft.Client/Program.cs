// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.ServiceBus.Management;
using Newtonsoft.Json;

namespace Microsoft.Coyote.Samples.CloudMessaging
{
    public static class Program
    {
        private static readonly List<Process> ServerProcesses = new List<Process>();
        private static readonly ConcurrentDictionary<string, bool> IsDone =
            new ConcurrentDictionary<string, bool>();

        private static volatile bool Retry = false;

        /// <summary>
        /// A simple client for the Raft service.
        /// </summary>
        public static async Task Main(string[] args)
        {
            string connectionString = string.Empty;
            string topicName = string.Empty;
            int numRequests = -1;
            int localClusterSize = -1;

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

                    case "--local-cluster-size":
                        if (int.TryParse(args[i + 1], out int localClusterSizeValue))
                        {
                            localClusterSize = localClusterSizeValue;
                        }

                        break;

                    case "--num-requests":
                        if (int.TryParse(args[i + 1], out int numRequestsValue))
                        {
                            numRequests = numRequestsValue;
                        }

                        break;
                }
            }

            if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(topicName) ||
                numRequests < 0 || localClusterSize < 0)
            {
                Console.WriteLine("Error: please specify the --connection-string, --topic-name, " +
                    "--local-cluster-size and --num-requests arguments.");
                return;
            }

            try
            {
                if (localClusterSize > 0)
                {
                    CreateRaftCluster(connectionString, topicName, localClusterSize);
                }

                for (int req = 0; req < numRequests; req++)
                {
                    IsDone.TryAdd($"request-{req}", false);
                }

                var managementClient = new ManagementClient(connectionString);
                if (!await managementClient.SubscriptionExistsAsync(topicName, "client"))
                {
                    await managementClient.CreateSubscriptionAsync(
                        new SubscriptionDescription(topicName, "client"));
                }

                Task sendMessagesTask = SendMessagesAsync(connectionString, topicName, numRequests);
                Task receiveMessagesTask = ReceiveMessagesAsync(connectionString, topicName, numRequests);
                await Task.WhenAll(sendMessagesTask, receiveMessagesTask);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} :: ex: {ex.Message}");
            }
        }

        private static async Task SendMessagesAsync(string connectionString, string topicName, int numRequests)
        {
            try
            {
                var topicClient = new TopicClient(connectionString, topicName);

                int request = 0;
                while (request < numRequests)
                {
                    string command = $"request-{request}";
                    Message message = new Message(Encoding.UTF8.GetBytes(
                        JsonConvert.SerializeObject(new ClientRequestEvent(command))))
                    {
                        Label = "ClientRequest"
                    };

                    await topicClient.SendAsync(message);
                    Console.WriteLine($"<Client> sent {command}.");

                    while (!IsDone[command])
                    {
                        await Task.Delay(100);

                        if (Retry)
                        {
                            Retry = false;
                            break;
                        }
                    }

                    if (IsDone[command])
                    {
                        request++;
                    }
                }

                await topicClient.CloseAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} :: ex: {ex.Message}");
            }
        }

        private static async Task ReceiveMessagesAsync(string connectionString, string topicName, int numRequests)
        {
            IMessageReceiver subscriptionReceiver = new MessageReceiver(connectionString,
                    EntityNameHelper.FormatSubscriptionPath(topicName, "client"),
                    ReceiveMode.ReceiveAndDelete);

            int request = 0;
            int retries = 0;
            while (request < numRequests)
            {
                // Receive the next message through Azure Service Bus.
                Message message = await subscriptionReceiver.ReceiveAsync(TimeSpan.FromMilliseconds(50));

                string nextCommand = $"request-{request}";
                if (message != null && message.Label == "ClientResponse")
                {
                    var response = JsonConvert.DeserializeObject<ClientResponseEvent>(
                        Encoding.UTF8.GetString(message.Body));
                    string command = response.Command;
                    if (command == nextCommand)
                    {
                        Console.WriteLine($"<Client> received response for {command}.");
                        IsDone[command] = true;
                        request++;
                        continue;
                    }
                }

                await Task.Delay(100);
                if (!IsDone[nextCommand] && retries == 100)
                {
                    Retry = true;
                    retries = 0;
                    continue;
                }

                retries++;
            }

            await subscriptionReceiver.CloseAsync();
        }

        #region infrastructure code
        private static void CreateRaftCluster(string connectionString, string topicName, int size)
        {
            int processId = Process.GetCurrentProcess().Id;
            var serverPath = Path.Combine(Path.GetDirectoryName(
                Assembly.GetExecutingAssembly().Location), "Raft.dll");

            for (int idx = 0; idx < size; idx++)
            {
                int serverId = idx;
                Process process = new Process();

                process.StartInfo.FileName = "dotnet";
                process.StartInfo.Arguments = $" {serverPath} --connection-string \"{connectionString}\" " +
                    $"--topic-name \"{topicName}\" --server-id {serverId} --num-servers {size} " +
                    $"--client-process-id {processId}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                process.OutputDataReceived += (sender, args) => Console.WriteLine($"Server-{serverId}: {args.Data}");

                process.Start();
                process.BeginOutputReadLine();

                Console.WriteLine($"<Client> started server process with id {process.Id}.");

                ServerProcesses.Add(process);
            }

            AppDomain.CurrentDomain.DomainUnload += KillServerProcesses;
            AppDomain.CurrentDomain.ProcessExit += KillServerProcesses;
            AppDomain.CurrentDomain.UnhandledException += KillServerProcesses;
            Console.CancelKeyPress += KillServerProcesses;
        }

        private static void KillServerProcesses(object sender, EventArgs e)
        {
            foreach (var process in ServerProcesses)
            {
                try
                {
                    process.Kill();
                    process.WaitForExit();

                    Console.WriteLine($"<Client> killed server process with id {process.Id}.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{DateTime.Now} :: Exception: {ex.Message}");
                }
            }
        }
        #endregion
    }
}
