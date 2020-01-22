// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Actors.Timers;

namespace Microsoft.Coyote.Samples.CoffeeMachine
{
    internal class RegisterClientEvent : Event
    {
        public ActorId Sender;

        public RegisterClientEvent(ActorId sender) { this.Sender = sender; }
    }

    internal class ReadPowerButtonEvent : Event { }

    internal class ReadWaterLevelEvent : Event { }

    internal class ReadHopperLevelEvent : Event { }

    internal class ReadWaterTemperatureEvent : Event { }

    internal class ReadPortaFilterCoffeeLevelEvent : Event { }

    internal class ReadDoorOpenEvent : Event { }

    /// <summary>
    /// The following events can be sent to turn things on or off, and can be returned from the matching
    /// read events.
    /// </summary>
    internal class PowerButtonEvent : Event
    {
        public bool PowerOn; // true means the power is on.

        public PowerButtonEvent(bool value) { this.PowerOn = value; }
    }

    internal class WaterHeaterButtonEvent : Event
    {
        public bool PowerOn; // true means the power is on.

        public WaterHeaterButtonEvent(bool value) { this.PowerOn = value; }
    }

    internal class GrinderButtonEvent : Event
    {
        public bool PowerOn; // true means the power is on.

        public GrinderButtonEvent(bool value) { this.PowerOn = value; }
    }

    internal class ShotButtonEvent : Event
    {
        public bool PowerOn; // true means the power is on, shot button produces 1 shot of espresso and turns off automatically, raising a ShowCompleteEvent press it multiple times to get multiple shots.

        public ShotButtonEvent(bool value) { this.PowerOn = value; }
    }

    internal class DumpGrindsButtonEvent : Event
    {
        public bool PowerOn; // true means the power is on, empties the PortaFilter and turns off automatically.

        public DumpGrindsButtonEvent(bool value) { this.PowerOn = value; }
    }

    /// <summary>
    /// The following events are returned when the matching read events are received.
    /// </summary>
    internal class WaterLevelEvent : Event
    {
        public double WaterLevel; // starts at 100% full and drops when shot button is on.

        public WaterLevelEvent(double value) { this.WaterLevel = value; }
    }

    internal class HopperLevelEvent : Event
    {
        public double HopperLevel; // starts at 100% full of beans, and drops when grinder is on.

        public HopperLevelEvent(double value) { this.HopperLevel = value; }
    }

    internal class WaterTemperatureEvent : Event
    {
        public double WaterTemperature; // starts at room temp, heats up to 100 when water heater is on.

        public WaterTemperatureEvent(double value) { this.WaterTemperature = value; }
    }

    internal class PortaFilterCoffeeLevelEvent : Event
    {
        public double CoffeeLevel; // starts out empty=0, it gets filled to 100% with ground coffee while grinder is on

        public PortaFilterCoffeeLevelEvent(double value) { this.CoffeeLevel = value; }
    }

    internal class ShotCompleteEvent : Event { }

    internal class WaterHotEvent : Event { }

    internal class WaterEmptyEvent : Event { }

    internal class HopperEmptyEvent : Event { }

    internal class DoorOpenEvent : Event
    {
        public bool Open; // true if open, a safety check to make sure machine is buttoned up properly before use.

        public DoorOpenEvent(bool value) { this.Open = value; }
    }

    /// <summary>
    /// This Actor models is a mock implementation of a set of sensors in the coffee machine, these sensors record a
    /// state independent from the coffee machine brain and that state persists no matter what
    /// happens with the coffee machine brain.  So this concept is modelled with a simple stateful
    /// dictionary and the sensor states are modelled as simple floating point values.
    /// </summary>
    [OnEventDoAction(typeof(RegisterClientEvent), nameof(OnRegisterClient))]
    [OnEventDoAction(typeof(ReadPowerButtonEvent), nameof(OnReadPowerButton))]
    [OnEventDoAction(typeof(ReadWaterLevelEvent), nameof(OnReadWaterLevel))]
    [OnEventDoAction(typeof(ReadHopperLevelEvent), nameof(OnReadHopperLevel))]
    [OnEventDoAction(typeof(ReadWaterTemperatureEvent), nameof(OnReadWaterTemperature))]
    [OnEventDoAction(typeof(ReadPortaFilterCoffeeLevelEvent), nameof(OnReadPortaFilterCoffeeLevel))]
    [OnEventDoAction(typeof(ReadDoorOpenEvent), nameof(OnReadDoorOpen))]
    [OnEventDoAction(typeof(PowerButtonEvent), nameof(OnPowerButton))]
    [OnEventDoAction(typeof(WaterHeaterButtonEvent), nameof(OnWaterHeaterButton))]
    [OnEventDoAction(typeof(GrinderButtonEvent), nameof(OnGrinderButton))]
    [OnEventDoAction(typeof(ShotButtonEvent), nameof(OnShotButton))]
    [OnEventDoAction(typeof(DumpGrindsButtonEvent), nameof(OnDumpGrindsButton))]
    [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimer))]
    internal class MockSensors : Actor
    {
        private ActorId Client;
        private bool PowerOn;
        private bool WaterHeaterButton;
        private double WaterLevel;
        private double HopperLevel;
        private double WaterTemperature;
        private bool GrinderButton;
        private double PortaFilterCoffeeLevel;
        private bool ShotButton;
        private bool DoorOpen;

        private TimerInfo WaterHeaterTimer;
        private TimerInfo CoffeeLevelTimer;
        private TimerInfo ShotTimer;
        private TimerInfo HopperLevelTimer;
        public bool RunSlowly;
        public static int Steps;

        internal class ConfigEvent : Event
        {
            public bool RunSlowly;

            public ConfigEvent(bool runSlowly)
            {
                this.RunSlowly = runSlowly;
            }
        }

        internal void OnRegisterClient(Event e)
        {
            if (e is RegisterClientEvent re)
            {
                this.Client = re.Sender;
            }
        }

        protected override Task OnInitializeAsync(Event initialEvent)
        {
            if (initialEvent is ConfigEvent ce)
            {
                this.RunSlowly = ce.RunSlowly;
            }

            // The use of randomness here makes this mock a more interesting test as it will
            // make sure the coffee machine handles these values correctly.
            this.WaterLevel = this.RandomInteger(100);
            this.HopperLevel = this.RandomInteger(100);
            this.WaterHeaterButton = false;
            this.WaterTemperature = this.RandomInteger(50) + 30;
            this.GrinderButton = false;
            this.PortaFilterCoffeeLevel = 0;
            this.ShotButton = false;
            this.DoorOpen = this.Random(5);

            this.WaterHeaterTimer = this.StartPeriodicTimer(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.1), "Heater");
            return base.OnInitializeAsync(initialEvent);
        }

        private void OnReadPowerButton()
        {
            this.SendEvent(this.Client, new PowerButtonEvent(this.PowerOn));
        }

        private void OnReadWaterLevel()
        {
            this.SendEvent(this.Client, new WaterLevelEvent(this.WaterLevel));
        }

        private void OnReadHopperLevel()
        {
            this.SendEvent(this.Client, new HopperLevelEvent(this.HopperLevel));
        }

        private void OnReadWaterTemperature()
        {
            this.SendEvent(this.Client, new WaterTemperatureEvent(this.WaterTemperature));
        }

        private void OnReadPortaFilterCoffeeLevel()
        {
            this.SendEvent(this.Client, new PortaFilterCoffeeLevelEvent(this.PortaFilterCoffeeLevel));
        }

        private void OnReadDoorOpen()
        {
            this.SendEvent(this.Client, new DoorOpenEvent(this.DoorOpen));
        }

        private void OnPowerButton(Event e)
        {
            if (e is PowerButtonEvent pe)
            {
                this.PowerOn = pe.PowerOn;
                if (!this.PowerOn)
                {
                    // master power override then also turns everything else off for safety!
                    this.WaterHeaterButton = false;
                    this.GrinderButton = false;
                    this.ShotButton = false;

                    if (this.CoffeeLevelTimer != null)
                    {
                        this.StopTimer(this.CoffeeLevelTimer);
                        this.CoffeeLevelTimer = null;
                    }

                    if (this.ShotTimer != null)
                    {
                        this.StopTimer(this.ShotTimer);
                        this.ShotTimer = null;
                    }

                    if (this.HopperLevelTimer != null)
                    {
                        this.StopTimer(this.HopperLevelTimer);
                        this.HopperLevelTimer = null;
                    }
                }
            }
        }

        private void OnWaterHeaterButton(Event e)
        {
            if (e is WaterHeaterButtonEvent we)
            {
                this.WaterHeaterButton = we.PowerOn;

                // should never turn on the heater when there is no water to heat
                if (this.WaterHeaterButton && this.WaterLevel <= 0)
                {
                    this.Assert(false, "Please do not turn on heater if there is no water");
                }
            }
        }

        private void OnGrinderButton(Event e)
        {
            if (e is GrinderButtonEvent ge)
            {
                this.GrinderButton = ge.PowerOn;
                this.OnGrinderButtonChanged();
            }
        }

        private void OnGrinderButtonChanged()
        {
            if (this.GrinderButton)
            {
                // should never turn on the grinder when there is no coffee to grind
                if (this.HopperLevel <= 0)
                {
                    this.Assert(false, "Please do not turn on grinder if there are no beans in the hopper");
                }

                // start monitoring the coffee level.
                this.CoffeeLevelTimer = this.StartPeriodicTimer(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.1), "Grinder");
            }
            else if (this.CoffeeLevelTimer != null)
            {
                this.StopTimer(this.CoffeeLevelTimer);
                this.CoffeeLevelTimer = null;
            }
        }

        private void OnShotButton(Event e)
        {
            if (e is ShotButtonEvent se)
            {
                this.ShotButton = se.PowerOn;

                if (this.ShotButton)
                {
                    // should never turn on the make shots button when there is no water
                    if (this.WaterLevel <= 0)
                    {
                        this.Assert(false, "Please do not turn on shot maker if there is no water");
                    }

                    // time the shot then send shot complete event.
                    this.ShotTimer = this.StartPeriodicTimer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), "Shot");
                }
                else if (this.ShotTimer != null)
                {
                    this.StopTimer(this.ShotTimer);
                    this.ShotTimer = null;
                }
            }
        }

        private void OnDumpGrindsButton(Event e)
        {
            if (e is DumpGrindsButtonEvent de && de.PowerOn)
            {
                // this is a toggle button, in no time grinds are dumped (just for simplicity)
                this.PortaFilterCoffeeLevel = 0;
            }
        }

        private void HandleTimer(Event e)
        {
            if (e is TimerElapsedEvent te)
            {
                string name = (string)te.Info.Payload;
                switch (name)
                {
                    case "Heater":
                        this.MonitorWaterTemperature();
                        break;
                    case "Grinder":
                        this.MonitorGrinder();
                        break;
                    case "Shot":
                        this.MonitorShot();
                        break;
                }
            }
        }

        private void MonitorWaterTemperature()
        {
            double temp = this.WaterTemperature;
            if (this.WaterHeaterButton)
            {
                // Note: when running in production mode we run forever, and it is fun
                // to watch the water heat up and cool down.   But in test mode this creates
                // too many async events to explore which makes the test slow.  So in test
                // mode we short circuit this process and jump straight to the boundry conditions.
                if (!this.RunSlowly && temp < 99)
                {
                    temp = 99;
                }

                // every time interval the temperature increases by 10 degrees up to 100 degrees
                if (temp < 100)
                {
                    temp = (int)temp + 10;
                    this.WaterTemperature = temp;
                    this.SendEvent(this.Client, new WaterTemperatureEvent(this.WaterTemperature));
                }
                else
                {
                    this.SendEvent(this.Client, new WaterHotEvent());
                }
            }
            else
            {
                // then it is cooling down to room temperature, more slowly.
                if (temp > 70)
                {
                    temp -= 0.1;
                    this.WaterTemperature = temp;
                }
            }
        }

        private void MonitorGrinder()
        {
            // Every time interval the portafilter fills 10%.
            // When it's full the grinder turns off automatically, unless the hopper is empty in which case
            // grinding does nothing!
            double hopperLevel = this.HopperLevel;
            if (hopperLevel > 0)
            {
                double level = this.PortaFilterCoffeeLevel;

                // Note: when running in production mode we run in real time, and it is fun
                // to watch the portafilter filling up.   But in test mode this creates
                // too many async events to explore which makes the test slow.  So in test
                // mode we short circuit this process and jump straight to the boundry conditions.
                if (!this.RunSlowly && level < 99)
                {
                    hopperLevel -= 98 - (int)level;
                    level = 99;
                }

                if (level < 100)
                {
                    level += 10;
                    this.PortaFilterCoffeeLevel = level;
                    this.SendEvent(this.Client, new PortaFilterCoffeeLevelEvent(this.PortaFilterCoffeeLevel));
                    if (level == 100)
                    {
                        // turning off the grinder is automatic
                        this.GrinderButton = false;
                        this.OnGrinderButtonChanged();
                    }
                }

                // and the hopper level drops by 0.1 percent
                hopperLevel -= 1;

                this.HopperLevel = hopperLevel;
            }

            if (this.HopperLevel <= 0)
            {
                hopperLevel = 0;
                this.SendEvent(this.Client, new HopperEmptyEvent());
                this.StopTimer(this.CoffeeLevelTimer);
                this.CoffeeLevelTimer = null;
            }
        }

        private void MonitorShot()
        {
            // one second of running water completes the shot.
            this.WaterLevel -= 1;
            if (this.WaterLevel > 0)
            {
                this.SendEvent(this.Client, new ShotCompleteEvent());
            }
            else
            {
                this.SendEvent(this.Client, new WaterEmptyEvent());
            }

            // automatically stop the water when shot is completed.
            if (this.ShotTimer != null)
            {
                this.StopTimer(this.ShotTimer);
                this.ShotTimer = null;
            }

            // turn off the water.
            this.ShotButton = false;
        }

        protected override Task OnEventHandledAsync(Event e)
        {
            Steps++;
            return base.OnEventHandledAsync(e);
        }
    }
}
