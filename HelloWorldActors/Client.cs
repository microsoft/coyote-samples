// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Specifications;

namespace Microsoft.Coyote.Samples.HelloWorld
{
    internal class Client : StateMachine
    {
        private TaskCompletionSource<bool> CompletionSource;
        private ActorId Server;
        private long MaxRequests = long.MaxValue;

        private long GreetMeCounter = 0;

        private const string English = "English";
        private const string HelloWorldEnglish = "Hello World!";

        internal class ConfigEvent : Event
        {
            public TaskCompletionSource<bool> CompletionSource;
            public ActorId OtherParty;
            public long MaxRequests;

            public ConfigEvent(TaskCompletionSource<bool> completionSource, ActorId otherParty, long maxRequests)
            {
                this.CompletionSource = completionSource;
                this.OtherParty = otherParty;
                this.MaxRequests = maxRequests;
            }
        }

        internal class GreetingProducedEvent : Event
        {
            public string Language;
            public string Greeting;
        }

        private class ReadyEvent : Event { }

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(ReadyEvent), typeof(Active))]
        private class Init : State { }

        private void InitOnEntry(Event e)
        {
            ConfigEvent configEvent = e as ConfigEvent;
            this.CompletionSource = configEvent.CompletionSource;
            this.Server = configEvent.OtherParty;
            this.MaxRequests = configEvent.MaxRequests;

            this.RaiseEvent(new ReadyEvent());
        }

        [OnEntry(nameof(ClientActiveEntry))]
        [OnEventDoAction(typeof(GreetingProducedEvent), nameof(HandleGreeting))]
        private class Active : State { }

        private void ClientActiveEntry()
        {
            Console.WriteLine($"Greeting in {English, -20}: {HelloWorldEnglish}");
            this.RequestGreeting();
        }

        private void HandleGreeting(Event e)
        {
            try
            {
                var greeting = (GreetingProducedEvent)e;
                Console.WriteLine($"Greeting in {greeting.Language, -20}: {greeting.Greeting}");

                Specification.Assert(greeting.Language != English, $"The starting Greeting in {English} was duplicated but this should never happen.");

                this.RequestGreeting();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception of type {ex.GetType()}: Message: {ex.Message}");

                this.TerminateMachines();
            }
        }

        private void RequestGreeting()
        {
           this.GreetMeCounter++;
           if (this.GreetMeCounter >= this.MaxRequests)
            {
                // terminate the client and the server
                this.TerminateMachines();
            }
            else
            {
                this.SendEvent(this.Server, new Server.GreetMeEvent(this.Id));
            }
        }

        private void TerminateMachines()
        {
            this.SendEvent(this.Server, HaltEvent.Instance);
            this.SendEvent(this.Id, HaltEvent.Instance);
            this.CompletionSource.TrySetResult(true);
        }
    }
}
