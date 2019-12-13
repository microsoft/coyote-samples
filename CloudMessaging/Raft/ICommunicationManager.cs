// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Coyote.Samples.CloudMessaging
{
    /// <summary>
    /// Interface that enables communication between remote <see cref="Server"/>
    /// instances of a Raft service.
    /// </summary>
    public interface ICommunicationManager
    {
        Task BroadcastVoteRequestsAsync(int term, int lastLogIndex, int lastLogTerm);

        Task SendVoteResponseAsync(string targetId, int term, bool voteGranted);

        Task SendAppendEntriesRequestAsync(string targetId, int term, int prevLogIndex,
            int prevLogTerm, List<Log> entries, int leaderCommit, string command);

        Task SendAppendEntriesResponseAsync(string targetId, int term, bool success, string command);

        Task SendClientResponseAsync(string command);
    }
}
