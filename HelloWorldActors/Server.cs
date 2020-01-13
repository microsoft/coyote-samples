// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Specifications;

namespace Microsoft.Coyote.Samples.HelloWorld
{
    [OnEventDoAction(typeof(GreetMeEvent), nameof(SendGreeting))]
    internal class Server : Actor
    {
        internal class GreetMeEvent : Event
        {
            public ActorId Requestor;

            public GreetMeEvent(ActorId requestor)
            {
                this.Requestor = requestor;
            }
        }

        private void SendGreeting(Event e)
        {
            ActorId client = (e as GreetMeEvent).Requestor;

            var index = this.RandomInteger(Translations.LanguagesCount);
            string language = Translations.Languages[index];
            string greeting = Translations.HelloWorldTexts[language];

            this.SendEvent(client, new Client.GreetingProducedEvent() { Language = language, Greeting = greeting });
        }
    }
}
