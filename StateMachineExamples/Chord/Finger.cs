// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.Coyote.Actors;

namespace Coyote.Examples.Chord
{
    public class Finger
    {
        public int Start;
        public int End;
        public ActorId Node;

        public Finger(int start, int end, ActorId node)
        {
            this.Start = start;
            this.End = end;
            this.Node = node;
        }
    }
}
