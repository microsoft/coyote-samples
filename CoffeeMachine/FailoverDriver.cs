// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Actors.Timers;

namespace Microsoft.Coyote.Samples.CoffeeMachine
{
    /// <summary>
    /// This class is designed to test how the CoffeeMachine handles "failover" or specifically,
    /// can it correctly "restart after failure" without getting into a bad state.  The CoffeeMachine
    /// will be randomly terminated.  The only thing the CoffeeMachine can depend on is
    /// the persistence of the state provided by the Sensors.
    /// </summary>
    internal class FailoverDriver : StateMachine
    {
        private ActorId SensorsId;
        private ActorId CoffeeMachineId;
        private bool RunForever;
        private int Iterations;
        private int MaxSteps;
        private int HaltSteps;
        private TimerInfo HaltTimer;
        private bool Halted;

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
            this.Halted = false;

            // Create a new CoffeeMachine instance
            this.CoffeeMachineId = this.CreateActor(typeof(CoffeeMachine), new CoffeeMachine.ConfigEvent(this.SensorsId));

            // Request a coffee!
            var shots = this.RandomInteger(3) + 1;
            this.SendEvent(this.CoffeeMachineId, new CoffeeMachine.MakeCoffeeEvent(this.Id, shots));

            // Setup a timer to randomly kill the coffee machine.   When the timer fires
            // we will restart the coffee machine and this is testing that the machine can
            // recover gracefully when that happens.
            if (this.MaxSteps > 0)
            {
                int steps = this.RandomInteger(this.MaxSteps * 9 / 8);
                MockSensors.Steps = 0;
                this.WriteLine("<FailoverDriver> Test will halt in {0} steps", steps);
                this.HaltSteps = steps;
                this.HaltTimer = this.StartPeriodicTimer(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.1));
            }
        }

        private Transition HandleTimer(Event e)
        {
            if (this.Halted)
            {
                // we are waiting for coffee machine to be really halted.
                if (CoffeeMachine.Halted)
                {
                    // ok, the CoffeeMachine really is halted now, so we can go to the stopped state.

                    if (this.HaltTimer != null)
                    {
                        this.StopTimer(this.HaltTimer);
                        this.HaltTimer = null;
                    }

                    return this.GotoState<Stopped>();
                }
            }
            else if (this.HaltSteps < MockSensors.Steps)
            {
                return this.OnStopTest(e);
            }

            return default;
        }

        [OnEntry(nameof(OnStopTest))]
        [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimer))]
        [IgnoreEvents(typeof(CoffeeMachine.CoffeeCompletedEvent))]
        internal class Stop : State { }

        internal Transition OnStopTest(Event e)
        {
            if (e is CoffeeMachine.CoffeeCompletedEvent)
            {
                this.MaxSteps = MockSensors.Steps;
                this.WriteLine("<FailoverDriver> Coffee completed in {0} steps", this.MaxSteps);
                MockSensors.Steps = 0;
            }

            if (!this.Halted)
            {
                // Halt the CoffeeMachine.  The failover test is then designed to check that
                // when we recreate a new CoffeeMachine it is able to pick up with the current
                // state of the sensors without getting confused.
                this.SendEvent(this.CoffeeMachineId, new CoffeeMachine.TerminateEvent());
                this.Halted = true;

                // need a timer to wait for coffee machine to be actually halted.
                if (this.HaltTimer == null)
                {
                    this.HaltTimer = this.StartPeriodicTimer(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.1));
                }
            }

            return default;
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
            this.Logger.WriteLine(format, args);
            Console.WriteLine(format, args);
        }
    }
}
