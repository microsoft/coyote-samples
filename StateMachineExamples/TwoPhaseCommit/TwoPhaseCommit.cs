// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.Coyote.Actors;

namespace Coyote.Examples.TwoPhaseCommit
{
    internal class TwoPhaseCommit : StateMachine
    {
        [Start]
        [OnEntry(nameof(InitOnEntry))]
        private class Init : State { }

        private void InitOnEntry()
        {
            var coordinator = this.CreateStateMachine(typeof(Coordinator));
            this.SendEvent(coordinator, new Coordinator.Config(2));

            var client1 = this.CreateStateMachine(typeof(Client));
            this.SendEvent(client1, new Client.Config(coordinator));

            var client2 = this.CreateStateMachine(typeof(Client));
            this.SendEvent(client2, new Client.Config(coordinator));
        }
    }
}
