// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Specifications;

namespace Microsoft.Coyote.Samples.CoffeeMachine
{
    internal class CoffeeMachine : StateMachine
    {
        private ActorId Client;
        private ActorId Sensors;
        private bool Heating;
        private double? WaterLevel;
        private double? HopperLevel;
        private bool? DoorOpen;
        private double? PortaFilterCoffeeLevel;
        private double? WaterTemperature;
        private int ShotsRequested;
        private double PreviousCoffeeLevel;
        private double PreviousShotCount;
        public static bool Halted;

        internal class ConfigEvent : Event
        {
            public ActorId Sensors;

            public ConfigEvent(ActorId sensors)
            {
                this.Sensors = sensors;
            }
        }

        internal class MakeCoffeeEvent : Event
        {
            public ActorId Sender;
            public int Shots;

            public MakeCoffeeEvent(ActorId sender, int shots)
            {
                this.Sender = sender;
                this.Shots = shots;
            }
        }

        internal class CoffeeCompletedEvent : Event
        {
            public bool Error;
        }

        internal class TerminateEvent : Event { }

        [Start]
        [OnEntry(nameof(OnInit))]
        [DeferEvents(typeof(MakeCoffeeEvent))]
        [OnEventDoAction(typeof(TerminateEvent), nameof(OnTerminate))]
        private class Init : State { }

        private Transition OnInit(Event e)
        {
            if (e is ConfigEvent configEvent)
            {
                Halted = false;
                this.WriteLine("<CoffeeMachine> initializing...");
                this.Sensors = configEvent.Sensors;
                // register this class as a client of the sensors.
                this.SendEvent(this.Sensors, new RegisterClientEvent(this.Id));
                // Use PushState so that TerminateEvent can be handled at any time in all the following states.
                return this.PushState<CheckSensors>();
            }

            return default;
        }

        [OnEntry(nameof(OnCheckSensors))]
        [DeferEvents(typeof(MakeCoffeeEvent))]
        [OnEventDoAction(typeof(PowerButtonEvent), nameof(OnPowerButton))]
        [OnEventDoAction(typeof(WaterLevelEvent), nameof(OnWaterLevel))]
        [OnEventDoAction(typeof(HopperLevelEvent), nameof(OnHopperLevel))]
        [OnEventDoAction(typeof(DoorOpenEvent), nameof(OnDoorOpen))]
        [OnEventDoAction(typeof(PortaFilterCoffeeLevelEvent), nameof(OnPortaFilterCoffeeLevel))]
        private class CheckSensors : State { }

        private void OnCheckSensors()
        {
            this.WriteLine("<CoffeeMachine> checking initial state of sensors...");
            // when this state machine starts it has to figure out the state of the sensors.
            this.SendEvent(this.Sensors, new ReadPowerButtonEvent());
        }

        private void OnPowerButton(Event e)
        {
            if (e is PowerButtonEvent pe)
            {
                if (!pe.PowerOn)
                {
                    // coffee machine was off already, so this is the easy case, simply turn it on!
                    this.SendEvent(this.Sensors, new PowerButtonEvent(true));
                }

                // make sure grinder, shot maker and water heater are off.
                this.SendEvent(this.Sensors, new GrinderButtonEvent(false));
                this.SendEvent(this.Sensors, new ShotButtonEvent(false));
                this.SendEvent(this.Sensors, new WaterHeaterButtonEvent(false));

                // need to check water and hopper levels and if the porta filter has coffee in it we need to dump those grinds.
                this.SendEvent(this.Sensors, new ReadWaterLevelEvent());
                this.SendEvent(this.Sensors, new ReadHopperLevelEvent());
                this.SendEvent(this.Sensors, new ReadDoorOpenEvent());
                this.SendEvent(this.Sensors, new ReadPortaFilterCoffeeLevelEvent());
            }
        }

        private Transition OnWaterLevel(Event e)
        {
            if (e is WaterLevelEvent we)
            {
                this.WaterLevel = we.WaterLevel;
                this.WriteLine("<CoffeeMachine> Water level is {0} %", (int)this.WaterLevel.Value);
                if ((int)this.WaterLevel.Value <= 0)
                {
                    this.WriteLine("<CoffeeMachine> Coffee machine is out of water");
                    return this.GotoState<RefillRequired>();
                }
            }

            return this.CheckInitialState();
        }

        private Transition OnHopperLevel(Event e)
        {
            if (e is HopperLevelEvent he)
            {
                this.HopperLevel = he.HopperLevel;
                this.WriteLine("<CoffeeMachine> Hopper level is {0} %", (int)this.HopperLevel.Value);
                if ((int)this.HopperLevel.Value == 0)
                {
                    this.WriteLine("<CoffeeMachine> Coffee machine is out of coffee beans");
                    return this.GotoState<RefillRequired>();
                }
            }

            return this.CheckInitialState();
        }

        private Transition OnDoorOpen(Event e)
        {
            if (e is DoorOpenEvent de)
            {
                this.DoorOpen = de.Open;
                if (this.DoorOpen.Value != false)
                {
                    this.WriteLine("<CoffeeMachine> Cannot safely operate coffee machine with the door open!");
                    return this.GotoState<Error>();
                }
            }

            return this.CheckInitialState();
        }

        private Transition OnPortaFilterCoffeeLevel(Event e)
        {
            if (e is PortaFilterCoffeeLevelEvent pe)
            {
                this.PortaFilterCoffeeLevel = pe.CoffeeLevel;
                if (pe.CoffeeLevel > 0)
                {
                    // dump these grinds because they could be old, we have no idea how long the coffee machine was off (no real time clock sensor).
                    this.WriteLine("<CoffeeMachine> Dumping old smelly grinds!");
                    this.SendEvent(this.Sensors, new DumpGrindsButtonEvent(true));
                }
            }

            return this.CheckInitialState();
        }

        private Transition CheckInitialState()
        {
            if (this.WaterLevel.HasValue && this.HopperLevel.HasValue && this.DoorOpen.HasValue && this.PortaFilterCoffeeLevel.HasValue)
            {
                return this.GotoState<HeatingWater>();
            }

            return default;
        }

        [OnEntry(nameof(OnStartHeating))]
        [DeferEvents(typeof(MakeCoffeeEvent))]
        [OnEventDoAction(typeof(WaterTemperatureEvent), nameof(MonitorWaterTemperature))]
        [OnEventDoAction(typeof(WaterHotEvent), nameof(OnWaterHot))]
        private class HeatingWater : State { }

        private Transition OnStartHeating()
        {
            // Start heater and keep monitoring the water temp till it reaches 100!
            this.WriteLine("<CoffeeMachine> Warming the water to 100 degrees");
            this.Monitor<LivenessMonitor>(new LivenessMonitor.BusyEvent());
            this.SendEvent(this.Sensors, new ReadWaterTemperatureEvent());
            return default;
        }

        private Transition OnWaterHot()
        {
            this.WriteLine("<CoffeeMachine> Coffee machine water temperature is now 100");
            if (this.Heating)
            {
                this.Heating = false;
                // turn off the heater so we don't overheat it!
                this.WriteLine("<CoffeeMachine> Turning off the water heater");
                this.SendEvent(this.Sensors, new WaterHeaterButtonEvent(false));
            }

            return this.GotoState<Ready>();
        }

        private Transition MonitorWaterTemperature(Event e)
        {
            if (e is WaterTemperatureEvent value)
            {
                this.WaterTemperature = value.WaterTemperature;

                if (this.WaterTemperature.Value >= 100)
                {
                    return this.OnWaterHot();
                }
                else
                {
                    if (!this.Heating)
                    {
                        this.Heating = true;
                        // turn on the heater and wait for WaterHotEvent.
                        this.WriteLine("<CoffeeMachine> Turning on the water heater");
                        this.SendEvent(this.Sensors, new WaterHeaterButtonEvent(true));
                    }
                }

                this.WriteLine("<CoffeeMachine> Coffee machine is warming up ({0} degrees)...", this.WaterTemperature);
            }

            return default;
        }

        [OnEntry(nameof(OnReady))]
        [IgnoreEvents(typeof(WaterLevelEvent))]
        [OnEventGotoState(typeof(MakeCoffeeEvent), typeof(MakingCoffee))]
        private class Ready : State { }

        private void OnReady()
        {
            this.Monitor<LivenessMonitor>(new LivenessMonitor.IdleEvent());
            this.WriteLine("<CoffeeMachine> Coffee machine is ready to make coffee (green light is on)");
        }

        [OnEntry(nameof(OnMakeCoffee))]
        private class MakingCoffee : State { }

        private Transition OnMakeCoffee(Event e)
        {
            if (e is MakeCoffeeEvent mc)
            {
                this.Client = mc.Sender;
                this.Monitor<LivenessMonitor>(new LivenessMonitor.BusyEvent());
                Console.WriteLine($"<CoffeeMachine> Coffee requested, shots={mc.Shots}");
                this.ShotsRequested = mc.Shots;

                // first we assume user placed a new cup in the machine, and so the shot count is zero.
                this.PreviousShotCount = 0;

                // grind beans until porta filter is full.
                // turn on shot button for desired time
                // dump the grinds, while checking for error conditions
                // like out of water or coffee beans.
                return this.GotoState<GrindingBeans>();
            }

            return default;
        }

        [OnEntry(nameof(OnGrindingBeans))]
        [OnEventDoAction(typeof(PortaFilterCoffeeLevelEvent), nameof(MonitorPortaFilter))]
        [OnEventDoAction(typeof(HopperLevelEvent), nameof(MonitorHopperLevel))]
        [OnEventDoAction(typeof(HopperEmptyEvent), nameof(OnHopperEmpty))]
        [IgnoreEvents(typeof(WaterHotEvent))]
        private class GrindingBeans : State { }

        private void OnGrindingBeans()
        {
            // grind beans until porta filter is full.
            this.WriteLine("<CoffeeMachine> Grinding beans...");
            // turn on the grinder!
            this.SendEvent(this.Sensors, new GrinderButtonEvent(true));
            // and keep monitoring the portafilter till it is full, and the bean level in case we get empty
            this.SendEvent(this.Sensors, new ReadHopperLevelEvent());
        }

        private Transition MonitorPortaFilter(Event e)
        {
            if (e is PortaFilterCoffeeLevelEvent pe)
            {
                if (pe.CoffeeLevel >= 100)
                {
                    this.WriteLine("<CoffeeMachine> PortaFilter is full");
                    this.SendEvent(this.Sensors, new GrinderButtonEvent(false));
                    return this.GotoState<MakingShots>();
                }
                else
                {
                    if (pe.CoffeeLevel != this.PreviousCoffeeLevel)
                    {
                        this.PreviousCoffeeLevel = pe.CoffeeLevel;
                        this.WriteLine("<CoffeeMachine> PortaFilter is {0} % full", pe.CoffeeLevel);
                    }
                }
            }

            return default;
        }

        private Transition MonitorHopperLevel(Event e)
        {
            if (e is HopperLevelEvent he)
            {
                if (he.HopperLevel <= 0)
                {
                    return this.OnHopperEmpty();
                }
                else
                {
                    this.SendEvent(this.Sensors, new ReadHopperLevelEvent());
                }
            }

            return default;
        }

        private Transition OnHopperEmpty()
        {
            this.WriteLine("<CoffeeMachine> hopper is empty!");
            this.SendEvent(this.Sensors, new GrinderButtonEvent(false));
            if (this.Client != null)
            {
                this.SendEvent(this.Client, new CoffeeCompletedEvent() { Error = true });
            }

            return this.GotoState<RefillRequired>();
        }

        [OnEntry(nameof(OnMakingShots))]
        [OnEventDoAction(typeof(WaterLevelEvent), nameof(OnMonitorWaterLevel))]
        [OnEventDoAction(typeof(ShotCompleteEvent), nameof(OnShotComplete))]
        [OnEventDoAction(typeof(WaterEmptyEvent), nameof(OnWaterEmpty))]
        [IgnoreEvents(typeof(HopperLevelEvent), typeof(HopperEmptyEvent))]
        private class MakingShots : State { }

        private void OnMakingShots()
        {
            // pour the shots.
            this.WriteLine("<CoffeeMachine> Making shots...");
            // turn on the grinder!
            this.SendEvent(this.Sensors, new ShotButtonEvent(true));
            // and keep monitoring ththe water is empty while we wait for ShotCompleteEvent.
            this.SendEvent(this.Sensors, new ReadWaterLevelEvent());
        }

        private Transition OnShotComplete()
        {
            this.PreviousShotCount++;
            if (this.PreviousShotCount >= this.ShotsRequested)
            {
                this.WriteLine("<CoffeeMachine> {0} shots completed and {1} shots requested!", this.PreviousShotCount, this.ShotsRequested);
                if (this.PreviousShotCount > this.ShotsRequested)
                {
                    this.Assert(false, "Made the wrong number of shots");
                }

                return this.GotoState<Cleanup>();
            }
            else
            {
                this.WriteLine("<CoffeeMachine> Shot count is {0}", this.PreviousShotCount);

                // request another shot!
                this.SendEvent(this.Sensors, new ShotButtonEvent(true));
            }

            return default;
        }

        private Transition OnWaterEmpty()
        {
            this.WriteLine("<CoffeeMachine> Water is empty!");
            // turn off the water pump
            this.SendEvent(this.Sensors, new ShotButtonEvent(false));
            if (this.Client != null)
            {
                this.SendEvent(this.Client, new CoffeeCompletedEvent() { Error = true });
            }

            return this.GotoState<RefillRequired>();
        }

        private Transition OnMonitorWaterLevel(Event e)
        {
            if (e is WaterLevelEvent we)
            {
                if (we.WaterLevel <= 0)
                {
                    return this.OnWaterEmpty();
                }
            }

            return default;
        }

        [OnEntry(nameof(OnCleanup))]
        [IgnoreEvents(typeof(WaterLevelEvent))]
        private class Cleanup : State { }

        private Transition OnCleanup()
        {
            // dump the grinds
            this.WriteLine("<CoffeeMachine> Dumping the grinds!");
            this.SendEvent(this.Sensors, new DumpGrindsButtonEvent(true));
            if (this.Client != null)
            {
                this.SendEvent(this.Client, new CoffeeCompletedEvent());
            }

            return this.GotoState<Ready>();
        }

        [OnEntry(nameof(OnRefillRequired))]
        [IgnoreEvents(typeof(MakeCoffeeEvent), typeof(WaterLevelEvent), typeof(HopperLevelEvent), typeof(DoorOpenEvent), typeof(PortaFilterCoffeeLevelEvent))]
        private class RefillRequired : State { }

        private void OnRefillRequired()
        {
            this.Monitor<LivenessMonitor>(new LivenessMonitor.IdleEvent());
            this.WriteLine("<CoffeeMachine> Coffee machine needs manual refilling of water and/or coffee beans!");
        }

        [OnEntry(nameof(OnError))]
        [IgnoreEvents(typeof(MakeCoffeeEvent), typeof(WaterLevelEvent), typeof(PortaFilterCoffeeLevelEvent))]
        private class Error : State { }

        private void OnError()
        {
            this.Monitor<LivenessMonitor>(new LivenessMonitor.IdleEvent());
            this.WriteLine("<CoffeeMachine> Coffee machine needs fixing!");
        }

        private Transition OnTerminate(Event e)
        {
            if (e is TerminateEvent te)
            {
                this.WriteLine("<CoffeeMachine> Coffee Machine Terminating...");
                this.SendEvent(this.Sensors, new PowerButtonEvent(false));
                return this.Halt();
            }

            return default;
        }

        protected override Task OnHaltAsync(Event e)
        {
            this.Monitor<LivenessMonitor>(new LivenessMonitor.IdleEvent());
            this.WriteLine("<CoffeeMachine> #################################################################");
            this.WriteLine("<CoffeeMachine> # Coffee Machine Halted                                         #");
            this.WriteLine("<CoffeeMachine> #################################################################");
            Console.WriteLine();
            Halted = true;
            return base.OnHaltAsync(e);
        }

        private void WriteLine(string format, params object[] args)
        {
            this.Logger.WriteLine(format, args);
            Console.WriteLine(format, args);
        }

        protected override Task OnEventUnhandledAsync(Event e, string state)
        {
            return base.OnEventUnhandledAsync(e, state);
        }
    }
}
