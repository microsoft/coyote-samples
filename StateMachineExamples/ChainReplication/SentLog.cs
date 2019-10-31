// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.Coyote.Actors;

namespace Coyote.Examples.ChainReplication
{
    public class SentLog
    {
        public int NextSeqId;
        public ActorId Client;
        public int Key;
        public int Value;

        public SentLog(int nextSeqId, ActorId client, int key, int val)
        {
            this.NextSeqId = nextSeqId;
            this.Client = client;
            this.Key = key;
            this.Value = val;
        }
    }
}
