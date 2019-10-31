// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.Coyote.Actors;

namespace Coyote.Examples.TwoPhaseCommit
{
    internal class PendingWriteRequest
    {
        public ActorId Client;
        public int SeqNum;
        public int Idx;
        public int Val;

        public PendingWriteRequest(int seqNum, int idx, int val)
        {
            this.SeqNum = seqNum;
            this.Idx = idx;
            this.Val = val;
        }

        public PendingWriteRequest(ActorId client, int idx, int val)
        {
            this.Client = client;
            this.Idx = idx;
            this.Val = val;
        }
    }
}
