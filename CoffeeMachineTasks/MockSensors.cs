// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tasks;

namespace Microsoft.Coyote.Samples.CoffeeMachineTasks
{
    /// <summary>
    /// This interface represents the state of the sensors in the coffee machine.  This is
    /// designed as an async interface to show how one might design a distributed system where
    /// messaging could be cross-process, or cross-device.  Perhaps there is a board in the
    /// coffee machine that contains the sensors, and another board somewhere else in the machine
    /// that runs the brain, so each sensor read/write is an async operation.  In the case of
    /// a cloud service you would also have async interface for messaging.  This async nature is
    /// where interesting bugs can show up and is where Coyote testing can be extremely useful.
    /// </summary>
    internal interface ISensors
    {
        ControlledTask<bool> GetPowerSwitchAsync();

        ControlledTask SetPowerSwitchAsync(bool value);

        ControlledTask<double> GetWaterLevelAsync();

        ControlledTask<double> GetHopperLevelAsync();

        ControlledTask<double> GetWaterTemperatureAsync();

        ControlledTask<double> GetPortaFilterCoffeeLevelAsync();

        ControlledTask<bool> GetReadDoorOpenAsync();

        ControlledTask SetWaterHeaterButtonAsync(bool value);

        ControlledTask SetGrinderButtonAsync(bool value);

        ControlledTask SetShotButtonAsync(bool value);

        ControlledTask SetDumpGrindsButtonAsync(bool value);

        ControlledTask TerminateAsync();

        /// <summary>
        /// An async event can be raised any time the water temperature changes.
        /// </summary>
        event EventHandler<double> WaterTemperatureChanged;

        /// <summary>
        /// An async event can be raised any time the water temperature reaches the right level for making coffee.
        /// </summary>
        event EventHandler<bool> WaterHot;

        /// <summary>
        /// An async event can be raised any time the coffee level changes in the porta filter.
        /// </summary>
        event EventHandler<double> PortaFilterCoffeeLevelChanged;

        /// <summary>
        /// Raised if we run out of coffee beans.
        /// </summary>
        event EventHandler<bool> HopperEmpty;

        /// <summary>
        /// Running a shot takes time, this event is raised when the shot is complete.
        /// </summary>
        event EventHandler<bool> ShotComplete;

        /// <summary>
        /// Raised if we run out of water.
        /// </summary>
        event EventHandler<bool> WaterEmpty;
    }

    /// <summary>
    /// This is a mock implementation of the ISensor interface.
    /// </summary>
    internal class MockSensors : ISensors
    {
        private readonly object SyncObject = new object();
        private bool PowerOn;
        private bool WaterHeaterButton;
        private double WaterLevel;
        private double HopperLevel;
        private double WaterTemperature;
        private bool GrinderButton;
        private double PortaFilterCoffeeLevel;
        private bool ShotButton;
        private readonly bool DoorOpen;

        private readonly ControlledTimer WaterHeaterTimer;
        private ControlledTimer CoffeeLevelTimer;
        private ControlledTimer ShotTimer;
        private ControlledTimer HopperLevelTimer;
        public bool RunSlowly;

        public event EventHandler<double> WaterTemperatureChanged;

        public event EventHandler<bool> WaterHot;

        public event EventHandler<double> PortaFilterCoffeeLevelChanged;

        public event EventHandler<bool> HopperEmpty;

        public event EventHandler<bool> ShotComplete;

        public event EventHandler<bool> WaterEmpty;

        public MockSensors(bool runSlowly)
        {
            this.RunSlowly = runSlowly;

            // The use of randomness here makes this mock a more interesting test as it will
            // make sure the coffee machine handles these values correctly.
            this.WaterLevel = ControlledRandomValueGenerator.GetNextInteger(100);
            this.HopperLevel = ControlledRandomValueGenerator.GetNextInteger(100);
            this.WaterHeaterButton = false;
            this.WaterTemperature = ControlledRandomValueGenerator.GetNextInteger(50) + 30;
            this.GrinderButton = false;
            this.PortaFilterCoffeeLevel = 0;
            this.ShotButton = false;
            this.DoorOpen = ControlledRandomValueGenerator.GetNextBoolean(5);
            this.WaterHeaterTimer = StartPeriodicTimer(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.1), new Action(this.MonitorWaterTemperature));
        }

        public ControlledTask TerminateAsync()
        {
            StopTimer(this.WaterHeaterTimer);
            StopTimer(this.CoffeeLevelTimer);
            StopTimer(this.ShotTimer);
            StopTimer(this.HopperLevelTimer);
            return ControlledTask.CompletedTask;
        }

        public async ControlledTask<bool> GetPowerSwitchAsync()
        {
            // to model real async behavior we insert a delay here.
            await ControlledTask.Delay(1);
            return this.PowerOn;
        }

        public async ControlledTask<double> GetWaterLevelAsync()
        {
            await ControlledTask.Delay(1);
            return this.WaterLevel;
        }

        public async ControlledTask<double> GetHopperLevelAsync()
        {
            await ControlledTask.Delay(1);
            return this.HopperLevel;
        }

        public async ControlledTask<double> GetWaterTemperatureAsync()
        {
            await ControlledTask.Delay(1);
            return this.WaterTemperature;
        }

        public async ControlledTask<double> GetPortaFilterCoffeeLevelAsync()
        {
            await ControlledTask.Delay(1);
            return this.PortaFilterCoffeeLevel;
        }

        public async ControlledTask<bool> GetReadDoorOpenAsync()
        {
            await ControlledTask.Delay(1);
            return this.DoorOpen;
        }

        public async ControlledTask SetPowerSwitchAsync(bool value)
        {
            await ControlledTask.Delay(1);
            ControlledTimer timer1 = null;
            ControlledTimer timer2 = null;
            ControlledTimer timer3 = null;

            // NOTE: you have to be very careful using locks with ControlledTasks.  You must ensure
            // the lock does not include a ControlledTask operation as that is not supported by
            // the Coyote runtime and can lead to deadlocks during testing.
            lock (this.SyncObject)
            {
                this.PowerOn = value;
                if (!this.PowerOn)
                {
                    // master power override then also turns everything else off for safety!
                    this.WaterHeaterButton = false;
                    this.GrinderButton = false;
                    this.ShotButton = false;

                    timer1 = this.CoffeeLevelTimer;
                    this.CoffeeLevelTimer = null;

                    timer2 = this.ShotTimer;
                    this.ShotTimer = null;

                    timer3 = this.HopperLevelTimer;
                    this.HopperLevelTimer = null;
                }
            }

            // This is why StopTimer must done outside of the lock.
            StopTimer(timer1);
            StopTimer(timer2);
            StopTimer(timer3);
        }

        public async ControlledTask SetWaterHeaterButtonAsync(bool value)
        {
            await ControlledTask.Delay(1);

            lock (this.SyncObject)
            {
                this.WaterHeaterButton = value;

                // should never turn on the heater when there is no water to heat
                if (this.WaterHeaterButton && this.WaterLevel <= 0)
                {
                    Specification.Assert(false, "Please do not turn on heater if there is no water");
                }
            }
        }

        public async ControlledTask SetGrinderButtonAsync(bool value)
        {
            await ControlledTask.Delay(1);
            this.OnGrinderButtonChanged(value);
        }

        private void OnGrinderButtonChanged(bool value)
        {
            ControlledTimer timer = null;

            lock (this.SyncObject)
            {
                timer = this.CoffeeLevelTimer;
                this.GrinderButton = value;
                if (this.GrinderButton)
                {
                    // should never turn on the grinder when there is no coffee to grind
                    if (this.HopperLevel <= 0)
                    {
                        Specification.Assert(false, "Please do not turn on grinder if there are no beans in the hopper");
                    }
                }
            }

            // ControlledTimer operations must be done outside the lock.
            if (value && timer == null)
            {
                // start monitoring the coffee level.
                timer = StartPeriodicTimer(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.1), new Action(this.MonitorGrinder));
            }
            else if (!value && timer != null)
            {
                StopTimer(timer);
                timer = null;
            }

            lock (this.SyncObject)
            {
                this.CoffeeLevelTimer = timer;
            }
        }

        public async ControlledTask SetShotButtonAsync(bool value)
        {
            await ControlledTask.Delay(1);

            ControlledTimer timer = null;
            lock (this.SyncObject)
            {
                timer = this.CoffeeLevelTimer;
                this.ShotButton = value;

                if (this.ShotButton)
                {
                    // should never turn on the make shots button when there is no water
                    if (this.WaterLevel <= 0)
                    {
                        Specification.Assert(false, "Please do not turn on shot maker if there is no water");
                    }
                }
            }

            // ControlledTimer operations must be done outside the lock.
            if (value && timer == null)
            {
                // start monitoring the coffee level.
                timer = StartPeriodicTimer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), new Action(this.MonitorShot));
            }
            else if (!value && timer != null)
            {
                StopTimer(timer);
                timer = null;
            }

            lock (this.SyncObject)
            {
                this.ShotTimer = timer;
            }
        }

        public async ControlledTask SetDumpGrindsButtonAsync(bool value)
        {
            await ControlledTask.Delay(1);
            if (value)
            {
                // this is a toggle button, in no time grinds are dumped (just for simplicity)
                this.PortaFilterCoffeeLevel = 0;
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
                    if (this.WaterTemperatureChanged != null)
                    {
                        this.WaterTemperatureChanged(this, this.WaterTemperature);
                    }
                }
                else
                {
                    if (this.WaterHot != null)
                    {
                        this.WaterHot(this, true);
                    }
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

            bool changed = false;
            bool turnOffGrinder = false;
            bool notifyEmpty = false;
            bool stopTimer = false;

            lock (this.SyncObject)
            {
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
                        Console.WriteLine("### HopperLevel: RunSlowly = {0}, level = {1}", this.RunSlowly, hopperLevel);
                        level = 99;
                    }

                    if (level < 100)
                    {
                        level += 10;
                        this.PortaFilterCoffeeLevel = level;
                        changed = true;
                        if (level == 100)
                        {
                            // turning off the grinder is automatic
                            turnOffGrinder = true;
                        }
                    }

                    // and the hopper level drops by 0.1 percent
                    hopperLevel -= 1;

                    this.HopperLevel = hopperLevel;
                }

                if (this.HopperLevel <= 0)
                {
                    hopperLevel = 0;
                    notifyEmpty = true;
                    stopTimer = true;
                }
            }

            if (turnOffGrinder)
            {
                this.OnGrinderButtonChanged(false);
            }

            if (notifyEmpty && this.HopperEmpty != null)
            {
                this.HopperEmpty(this, true);
            }

            if (stopTimer && this.CoffeeLevelTimer != null)
            {
                StopTimer(this.CoffeeLevelTimer);
                this.CoffeeLevelTimer = null;
            }

            // event callbacks should not be inside the lock otherwise we could get deadlocks.
            if (changed && this.PortaFilterCoffeeLevelChanged != null)
            {
                this.PortaFilterCoffeeLevelChanged(this, this.PortaFilterCoffeeLevel);
            }

            if (this.HopperLevel <= 0 && this.HopperEmpty != null)
            {
                this.HopperEmpty(this, true);
            }
        }

        private void MonitorShot()
        {
            // one second of running water completes the shot.
            lock (this.SyncObject)
            {
                this.WaterLevel -= 1;
                // turn off the water.
                this.ShotButton = false;
            }

            // automatically stop the water when shot is completed.
            if (this.ShotTimer != null)
            {
                StopTimer(this.ShotTimer);
                this.ShotTimer = null;
            }

            if (this.WaterLevel > 0)
            {
                if (this.ShotComplete != null)
                {
                    this.ShotComplete(this, true);
                }
            }
            else
            {
                if (this.WaterEmpty != null)
                {
                    this.WaterEmpty(this, true);
                }
            }
        }

        private static void StopTimer(ControlledTimer timer)
        {
            if (timer != null)
            {
                timer.Stop();
            }
        }

        private static ControlledTimer StartPeriodicTimer(TimeSpan startDelay, TimeSpan interval, Action handler)
        {
            return new ControlledTimer(startDelay, interval, handler);
        }
    }
}
