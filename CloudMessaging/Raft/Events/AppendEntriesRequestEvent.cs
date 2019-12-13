// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.Coyote.Samples.CloudMessaging
{
    /// <summary>
    /// Initiated by leaders to replicate log entries and
    /// to provide a form of heartbeat.
    /// </summary>
    [DataContract]
    public class AppendEntriesRequestEvent : Event
    {
        /// <summary>
        /// The leaders term.
        /// </summary>
        [DataMember]
        public readonly int Term;

        /// <summary>
        /// The leader id so follower can redirect requests.
        /// </summary>
        [DataMember]
        public readonly string LeaderId;

        /// <summary>
        /// The index of the log entry immediately preceding new ones.
        /// </summary>
        [DataMember]
        public readonly int PrevLogIndex;

        /// <summary>
        /// The term of the <see cref="PrevLogIndex"/> entry.
        /// </summary>
        [DataMember]
        public readonly int PrevLogTerm;

        /// <summary>
        /// Log entries to store (empty for heartbeat; may send more than one for efficiency).
        /// </summary>
        [DataMember]
        public readonly List<Log> Entries;

        /// <summary>
        /// The leader commit index.
        /// </summary>
        [DataMember]
        public readonly int LeaderCommit;

        /// <summary>
        /// The client request command, if any.
        /// </summary>
        [DataMember]
        public readonly string Command;

        public AppendEntriesRequestEvent(int term, string leaderId, int prevLogIndex,
            int prevLogTerm, List<Log> entries, int leaderCommit, string command)
        {
            this.Term = term;
            this.LeaderId = leaderId;
            this.PrevLogIndex = prevLogIndex;
            this.PrevLogTerm = prevLogTerm;
            this.Entries = entries;
            this.LeaderCommit = leaderCommit;
            this.Command = command;
        }
    }
}
