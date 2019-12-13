// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Samples.CloudMessaging
{
    /// <summary>
    /// Event that configures a Raft server.
    /// </summary>
    public class SetupServerEvent : Event
    {
        public readonly IServerManager ServerManager;
        public readonly ICommunicationManager CommunicationManager;

        public SetupServerEvent(IServerManager serverManager, ICommunicationManager communicationManager)
        {
            this.ServerManager = serverManager;
            this.CommunicationManager = communicationManager;
        }
    }
}
