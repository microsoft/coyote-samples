// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Specifications;

namespace Microsoft.Coyote.Samples.CoffeeMachineActors
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

        internal class ConfigEvent : Event
        {
            public ActorId Sensors;
            public ActorId Client;

            public ConfigEvent(ActorId sensors, ActorId client)
            {
                this.Sensors = sensors;
                this.Client = client;
            }
        }

        internal class MakeCoffeeEvent : Event
        {
            public int Shots;

            public MakeCoffeeEvent(int shots)
            {
                this.Shots = shots;
            }
        }

        internal class CoffeeCompletedEvent : Event
        {
            public bool Error;
        }

        internal class TerminateEvent : Event { }

        internal class HaltedEvent : Event { }

        [Start]
        [OnEntry(nameof(OnInit))]
        [DeferEvents(typeof(MakeCoffeeEvent))]
        [OnEventDoAction(typeof(TerminateEvent), nameof(OnTerminate))]
        private class Init : State { }

        private void OnInit(Event e)
        {
            if (e is ConfigEvent configEvent)
            {
                this.WriteLine("initializing...");
                this.Client = configEvent.Client;
                this.Sensors = configEvent.Sensors;
                // register this class as a client of the sensors.
                this.SendEvent(this.Sensors, new RegisterClientEvent(this.Id));
                // Use PushState so that TerminateEvent can be handled at any time in all the following states.
                this.RaisePushStateEvent<CheckSensors>();
            }
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
            this.WriteLine("checking initial state of sensors...");
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

        private void OnWaterLevel(Event e)
        {
            if (e is WaterLevelEvent we)
            {
                this.WaterLevel = we.WaterLevel;
                this.WriteLine("Water level is {0} %", (int)this.WaterLevel.Value);
                if ((int)this.WaterLevel.Value <= 0)
                {
                    this.WriteLine("Coffee machine is out of water");
                    this.RaiseGotoStateEvent<RefillRequired>();
                }
            }

            this.CheckInitialState();
        }

        private void OnHopperLevel(Event e)
        {
            if (e is HopperLevelEvent he)
            {
                this.HopperLevel = he.HopperLevel;
                this.WriteLine("Hopper level is {0} %", (int)this.HopperLevel.Value);
                if ((int)this.HopperLevel.Value == 0)
                {
                    this.WriteLine("Coffee machine is out of coffee beans");
                    this.RaiseGotoStateEvent<RefillRequired>();
                }
            }

            this.CheckInitialState();
        }

        private void OnDoorOpen(Event e)
        {
            if (e is DoorOpenEvent de)
            {
                this.DoorOpen = de.Open;
                if (this.DoorOpen.Value != false)
                {
                    this.WriteLine("Cannot safely operate coffee machine with the door open!");
                    this.RaiseGotoStateEvent<Error>();
                }
            }

            this.CheckInitialState();
        }

        private void OnPortaFilterCoffeeLevel(Event e)
        {
            if (e is PortaFilterCoffeeLevelEvent pe)
            {
                this.PortaFilterCoffeeLevel = pe.CoffeeLevel;
                if (pe.CoffeeLevel > 0)
                {
                    // dump these grinds because they could be old, we have no idea how long the coffee machine was off (no real time clock sensor).
                    this.WriteLine("Dumping old smelly grinds!");
                    this.SendEvent(this.Sensors, new DumpGrindsButtonEvent(true));
                }
            }

            this.CheckInitialState();
        }

        private void CheckInitialState()
        {
            if (this.WaterLevel.HasValue && this.HopperLevel.HasValue && this.DoorOpen.HasValue && this.PortaFilterCoffeeLevel.HasValue)
            {
                this.RaiseGotoStateEvent<HeatingWater>();
            }
        }

        [OnEntry(nameof(OnStartHeating))]
        [DeferEvents(typeof(MakeCoffeeEvent))]
        [OnEventDoAction(typeof(WaterTemperatureEvent), nameof(MonitorWaterTemperature))]
        [OnEventDoAction(typeof(WaterHotEvent), nameof(OnWaterHot))]
        private class HeatingWater : State { }

        private void OnStartHeating()
        {
            // Start heater and keep monitoring the water temp till it reaches 100!
            this.WriteLine("Warming the water to 100 degrees");
            this.Monitor<LivenessMonitor>(new LivenessMonitor.BusyEvent());
            this.SendEvent(this.Sensors, new ReadWaterTemperatureEvent());
        }

        private void OnWaterHot()
        {
            this.WriteLine("Coffee machine water temperature is now 100");
            if (this.Heating)
            {
                this.Heating = false;
                // turn off the heater so we don't overheat it!
                this.WriteLine("Turning off the water heater");
                this.SendEvent(this.Sensors, new WaterHeaterButtonEvent(false));
            }

            this.RaiseGotoStateEvent<Ready>();
        }

        private void MonitorWaterTemperature(Event e)
        {
            if (e is WaterTemperatureEvent value)
            {
                this.WaterTemperature = value.WaterTemperature;

                if (this.WaterTemperature.Value >= 100)
                {
                    this.OnWaterHot();
                }
                else
                {
                    if (!this.Heating)
                    {
                        this.Heating = true;
                        // turn on the heater and wait for WaterHotEvent.
                        this.WriteLine("Turning on the water heater");
                        this.SendEvent(this.Sensors, new WaterHeaterButtonEvent(true));
                    }
                }

                this.WriteLine("Coffee machine is warming up ({0} degrees)...", (int)this.WaterTemperature);
            }
        }

        [OnEntry(nameof(OnReady))]
        [IgnoreEvents(typeof(WaterLevelEvent))]
        [OnEventGotoState(typeof(MakeCoffeeEvent), typeof(MakingCoffee))]
        private class Ready : State { }

        private void OnReady()
        {
            this.Monitor<LivenessMonitor>(new LivenessMonitor.IdleEvent());
            this.WriteLine("Coffee machine is ready to make coffee (green light is on)");
        }

        [OnEntry(nameof(OnMakeCoffee))]
        private class MakingCoffee : State { }

        private void OnMakeCoffee(Event e)
        {
            if (e is MakeCoffeeEvent mc)
            {
                this.Monitor<LivenessMonitor>(new LivenessMonitor.BusyEvent());
                this.WriteLine($"Coffee requested, shots={mc.Shots}");
                this.ShotsRequested = mc.Shots;

                // first we assume user placed a new cup in the machine, and so the shot count is zero.
                this.PreviousShotCount = 0;

                // grind beans until porta filter is full.
                // turn on shot button for desired time
                // dump the grinds, while checking for error conditions
                // like out of water or coffee beans.
                this.RaiseGotoStateEvent<GrindingBeans>();
            }
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
            this.WriteLine("Grinding beans...");
            // turn on the grinder!
            this.SendEvent(this.Sensors, new GrinderButtonEvent(true));
            // and keep monitoring the portafilter till it is full, and the bean level in case we get empty
            this.SendEvent(this.Sensors, new ReadHopperLevelEvent());
        }

        private void MonitorPortaFilter(Event e)
        {
            if (e is PortaFilterCoffeeLevelEvent pe)
            {
                if (pe.CoffeeLevel >= 100)
                {
                    this.WriteLine("PortaFilter is full");
                    this.SendEvent(this.Sensors, new GrinderButtonEvent(false));
                    this.RaiseGotoStateEvent<MakingShots>();
                }
                else
                {
                    if (pe.CoffeeLevel != this.PreviousCoffeeLevel)
                    {
                        this.PreviousCoffeeLevel = pe.CoffeeLevel;
                        this.WriteLine("PortaFilter is {0} % full", pe.CoffeeLevel);
                    }
                }
            }
        }

        private void MonitorHopperLevel(Event e)
        {
            if (e is HopperLevelEvent he)
            {
                if (he.HopperLevel == 0)
                {
                    this.OnHopperEmpty();
                }
                else
                {
                    this.SendEvent(this.Sensors, new ReadHopperLevelEvent());
                }
            }
        }

        private void OnHopperEmpty()
        {
            this.WriteLine("hopper is empty!");
            this.SendEvent(this.Sensors, new GrinderButtonEvent(false));
            this.RaiseGotoStateEvent<RefillRequired>();
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
            this.WriteLine("Making shots...");
            // turn on the grinder!
            this.SendEvent(this.Sensors, new ShotButtonEvent(true));
            // and keep monitoring ththe water is empty while we wait for ShotCompleteEvent.
            this.SendEvent(this.Sensors, new ReadWaterLevelEvent());
        }

        private void OnShotComplete()
        {
            this.PreviousShotCount++;
            if (this.PreviousShotCount >= this.ShotsRequested)
            {
                this.WriteLine("{0} shots completed and {1} shots requested!", this.PreviousShotCount, this.ShotsRequested);
                if (this.PreviousShotCount > this.ShotsRequested)
                {
                    this.Assert(false, "Made the wrong number of shots");
                }

                this.RaiseGotoStateEvent<Cleanup>();
            }
            else
            {
                this.WriteLine("Shot count is {0}", this.PreviousShotCount);

                // request another shot!
                this.SendEvent(this.Sensors, new ShotButtonEvent(true));
            }
        }

        private void OnWaterEmpty()
        {
            this.WriteLine("Water is empty!");
            // turn off the water pump
            this.SendEvent(this.Sensors, new ShotButtonEvent(false));
            this.RaiseGotoStateEvent<RefillRequired>();
        }

        private void OnMonitorWaterLevel(Event e)
        {
            if (e is WaterLevelEvent we)
            {
                if (we.WaterLevel <= 0)
                {
                    this.OnWaterEmpty();
                }
            }
        }

        [OnEntry(nameof(OnCleanup))]
        [IgnoreEvents(typeof(WaterLevelEvent))]
        private class Cleanup : State { }

        private void OnCleanup()
        {
            // dump the grinds
            this.WriteLine("Dumping the grinds!");
            this.SendEvent(this.Sensors, new DumpGrindsButtonEvent(true));
            if (this.Client != null)
            {
                this.SendEvent(this.Client, new CoffeeCompletedEvent());
            }

            this.RaiseGotoStateEvent<Ready>();
        }

        [OnEntry(nameof(OnRefillRequired))]
        [OnEventDoAction(typeof(TerminateEvent), nameof(OnTerminate))]
        [IgnoreEvents(typeof(MakeCoffeeEvent), typeof(WaterLevelEvent), typeof(HopperLevelEvent), typeof(DoorOpenEvent), typeof(PortaFilterCoffeeLevelEvent))]
        private class RefillRequired : State { }

        private void OnRefillRequired()
        {
            if (this.Client != null)
            {
                this.SendEvent(this.Client, new CoffeeCompletedEvent() { Error = true });
            }

            this.Monitor<LivenessMonitor>(new LivenessMonitor.IdleEvent());
            this.WriteLine("Coffee machine needs manual refilling of water and/or coffee beans!");
        }

        [OnEntry(nameof(OnError))]
        [IgnoreEvents(typeof(MakeCoffeeEvent), typeof(WaterLevelEvent), typeof(PortaFilterCoffeeLevelEvent))]
        private class Error : State { }

        private void OnError()
        {
            if (this.Client != null)
            {
                this.SendEvent(this.Client, new CoffeeCompletedEvent() { Error = true });
            }

            this.Monitor<LivenessMonitor>(new LivenessMonitor.IdleEvent());
            this.WriteLine("Coffee machine needs fixing!");
        }

        private void OnTerminate(Event e)
        {
            if (e is TerminateEvent te)
            {
                this.WriteLine("Coffee Machine Terminating...");
                this.SendEvent(this.Sensors, new PowerButtonEvent(false));
                this.RaiseHaltEvent();
            }
        }

        protected override Task OnHaltAsync(Event e)
        {
            this.Monitor<LivenessMonitor>(new LivenessMonitor.IdleEvent());
            this.WriteLine("#################################################################");
            this.WriteLine("# Coffee Machine Halted                                         #");
            this.WriteLine("#################################################################");
            Console.WriteLine();
            if (this.Client != null)
            {
                this.SendEvent(this.Client, new HaltedEvent());
            }

            return base.OnHaltAsync(e);
        }

        private void WriteLine(string format, params object[] args)
        {
            string msg = string.Format(format, args);
            msg = "<CoffeeMachine> " + msg;
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
