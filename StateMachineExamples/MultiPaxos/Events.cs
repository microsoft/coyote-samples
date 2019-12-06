// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.Coyote;
using Microsoft.Coyote.Actors;

namespace Coyote.Examples.MultiPaxos
{
    internal class Local : Event { }

    internal class Success : Event { }

    internal class GoPropose : Event { }

    internal class Response : Event { }
}
