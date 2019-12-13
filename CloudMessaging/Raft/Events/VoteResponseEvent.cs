// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;

namespace Microsoft.Coyote.Samples.CloudMessaging
{
    /// <summary>
    /// Response to a vote request.
    /// </summary>
    [DataContract]
    public class VoteResponseEvent : Event
    {
        /// <summary>
        /// The current term for the candidate to update itself.
        /// </summary>
        [DataMember]
        public readonly int Term;

        /// <summary>
        /// True means that the candidate received the vote.
        /// </summary>
        [DataMember]
        public readonly bool VoteGranted;

        public VoteResponseEvent(int term, bool voteGranted)
        {
            this.Term = term;
            this.VoteGranted = voteGranted;
        }
    }
}
