// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;

namespace Microsoft.Coyote.Samples.CloudMessaging
{
    /// <summary>
    /// Response to an append entries request.
    /// </summary>
    [DataContract]
    public class AppendEntriesResponseEvent : Event
    {
        /// <summary>
        /// The current term for the leader to update itself.
        /// </summary>
        [DataMember]
        public readonly int Term;

        /// <summary>
        /// True if the follower contained entry matching PrevLogIndex and PrevLogTerm.
        /// </summary>
        [DataMember]
        public readonly bool Success;

        /// <summary>
        /// The server id so leader can update its state.
        /// </summary>
        [DataMember]
        public readonly string ServerId;

        /// <summary>
        /// The client request command, if any.
        /// </summary>
        [DataMember]
        public readonly string Command;

        public AppendEntriesResponseEvent(int term, bool success, string serverId, string command)
        {
            this.Term = term;
            this.Success = success;
            this.ServerId = serverId;
            this.Command = command;
        }
    }
}
