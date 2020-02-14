// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Actors.Timers;

namespace Microsoft.Coyote.Samples.CoffeeMachineActors
{
    /// <summary>
    /// This class is designed to test how the CoffeeMachine handles "failover" or specifically,
    /// can it correctly "restart after failure" without getting into a bad state.  The CoffeeMachine
    /// will be randomly terminated.  The only thing the CoffeeMachine can depend on is
    /// the persistence of the state provided by the MockSensors.
    /// </summary>
    internal class FailoverDriver : StateMachine
    {
        private ActorId SensorsId;
        private ActorId CoffeeMachineId;
        private bool RunForever;
        private int Iterations;
        private TimerInfo HaltTimer;

        internal class ConfigEvent : Event
        {
            public bool RunForever;

            public ConfigEvent(bool runForever)
            {
                this.RunForever = runForever;
            }
        }

        internal class StartTestEvent : Event { }

        [Start]
        [OnEntry(nameof(OnInit))]
        [OnEventGotoState(typeof(StartTestEvent), typeof(Test))]
        internal class Init : State { }

        internal void OnInit(Event e)
        {
            if (e is ConfigEvent ce)
            {
                this.RunForever = ce.RunForever;
            }

            // Create the persistent sensor state.
            this.SensorsId = this.CreateActor(typeof(MockSensors), new MockSensors.ConfigEvent(this.RunForever));
        }

        [OnEntry(nameof(OnStartTest))]
        [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimer))]
        [OnEventGotoState(typeof(CoffeeMachine.CoffeeCompletedEvent), typeof(Stop))]
        internal class Test : State { }

        internal void OnStartTest()
        {
            this.WriteLine("#################################################################");
            this.WriteLine("starting new CoffeeMachine.");
            // Create a new CoffeeMachine instance
            this.CoffeeMachineId = this.CreateActor(typeof(CoffeeMachine), new CoffeeMachine.ConfigEvent(this.SensorsId, this.Id));

            // Request a coffee!
            var shots = this.RandomInteger(3) + 1;
            this.SendEvent(this.CoffeeMachineId, new CoffeeMachine.MakeCoffeeEvent(shots));

            // Setup a timer to randomly kill the coffee machine.   When the timer fires
            // we will restart the coffee machine and this is testing that the machine can
            // recover gracefully when that happens.
            this.HaltTimer = this.StartTimer(TimeSpan.FromSeconds(this.RandomInteger(7) + 1));
        }

        private Transition HandleTimer()
        {
            return this.GotoState<Stop>();
        }

        internal Transition OnStopTest(Event e)
        {
            if (this.HaltTimer != null)
            {
                this.StopTimer(this.HaltTimer);
                this.HaltTimer = null;
            }

            if (e is CoffeeMachine.CoffeeCompletedEvent ce)
            {
                if (ce.Error)
                {
                    this.WriteLine("CoffeeMachine reported an error.");
                    this.WriteLine("Test is complete, press ENTER to continue...");
                    this.RunForever = false; // no point trying to make more coffee.
                }
                else
                {
                    this.WriteLine("CoffeeMachine completed the job.");
                }

                return this.GotoState<Stopped>();
            }
            else
            {
                // Halt the CoffeeMachine.  HaltEvent is async and we must ensure the
                // CoffeeMachine is really halted before we create a new one because MockSensors
                // will get confused if two CoffeeMachines are running at the same time.
                // So we've implemented a terminate handshake here.  We send event to the CoffeeMachine
                // to terminate, and it sends back a HaltedEvent when it really has been halted.
                this.WriteLine("forcing termination of CoffeeMachine.");
                this.SendEvent(this.CoffeeMachineId, new CoffeeMachine.TerminateEvent());
            }

            return default;
        }

        [OnEntry(nameof(OnStopTest))]
        [OnEventDoAction(typeof(CoffeeMachine.HaltedEvent), nameof(OnHalted))]
        [IgnoreEvents(typeof(CoffeeMachine.CoffeeCompletedEvent))]
        internal class Stop : State { }

        internal Transition OnHalted()
        {
            // ok, the CoffeeMachine really is halted now, so we can go to the stopped state.
            return this.GotoState<Stopped>();
        }

        [OnEntry(nameof(OnStopped))]
        internal class Stopped : State { }

        private Transition OnStopped()
        {
            if (this.RunForever || this.Iterations == 0)
            {
                this.Iterations += 1;
                // Run another CoffeeMachine instance!
                return this.GotoState<Test>();
            }
            else
            {
                return this.Halt();
            }
        }

        private void WriteLine(string format, params object[] args)
        {
            string msg = string.Format(format, args);
            msg = "<FailoverDriver> " + msg;
            this.Logger.WriteLine(msg);
            Console.WriteLine(msg);
        }

        protected override Task OnEventUnhandledAsync(Event e, string state)
        {
            this.WriteLine("### Unhandled event {0} in state {1}", e.GetType().FullName, state);
            return base.OnEventUnhandledAsync(e, state);
        }
    }
}
