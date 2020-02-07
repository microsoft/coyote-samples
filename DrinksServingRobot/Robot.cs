﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Actors.Timers;
using Microsoft.Coyote.Specifications;

namespace Microsoft.Coyote.Samples.DrinksServingRobot
{
    internal class Robot : StateMachine
    {
        internal ActorId CreatorId; // the Id of the Actor who created this instance.

        internal ActorId NavigatorId { get; set; }

        internal bool RunForever;

        private static readonly Location StartingLocation = new Location(1, 1);
        private Location Coordinates = StartingLocation;
        private List<Location> Route;

        private DrinkOrder CurrentOrder;
        private bool DrinkOrderConfirmed;

        internal const int MoveDuration = 1;
        internal const int ServingDuration = 2;
        internal const int RetreatingDuration = 1;

        private readonly Dictionary<string, TimerInfo> Timers = new Dictionary<string, TimerInfo>
        {
            { "MoveTimer", null }
        };

        private readonly Dictionary<string, Event> TimerSpecificEvents = new Dictionary<string, Event>
        {
            { "MoveTimer", new MoveTimerElapsedEvent() }
        };

        internal class ConfigEvent : Event
        {
            internal readonly bool RunForever;
            internal readonly ActorId CreatorId;

            public ConfigEvent(bool runForever, ActorId creatorId)
            {
                this.RunForever = runForever;
                this.CreatorId = creatorId;
            }
        }

        internal class RobotReadyEvent : Event
        {
        }

        internal class NavigatorResetEvent : Event { }

        internal class MoveTimerElapsedEvent : Event { }

        internal class CompletedEvent : Event { }

        [Start]
        [OnEntry(nameof(OnInit))]
        [OnEventDoAction(typeof(TimerElapsedEvent), nameof(DispatchTimer))]
        [OnEventDoAction(typeof(Navigator.RegisterNavigatorEvent), nameof(OnSetNavigator))]
        [DeferEvents(typeof(Navigator.DrinkOrderProducedEvent))]
        internal class Init : State { }

        internal void OnInit(Event e)
        {
            if (e is ConfigEvent ce)
            {
                this.RunForever = ce.RunForever;
                this.CreatorId = ce.CreatorId;
            }
        }

        private Transition OnSetNavigator(Event e)
        {
            if (e is Navigator.RegisterNavigatorEvent sne)
            {
                // Note: the whole point of this sample is to test failover of the Navigator.
                // The Robot is designed to be robust in the face of failover, and that means
                // it needs to continue on with the new navigator object.
                if (this.NavigatorId == null)
                {
                    this.NavigatorId = sne.NewNavigatorId;
                    return this.PushState<Active>();
                }
                else
                {
                    this.WriteLine("<Robot> received a new Navigator, and pending drink order={0}!!!", this.DrinkOrderConfirmed);

                    // continue on with the new navigator.
                    this.NavigatorId = sne.NewNavigatorId;

                    if (this.DrinkOrderConfirmed)
                    {
                        // stop any current driving and wait for DrinkOrderProducedEvent from new navigator
                        // as it restarts the previous drink order request.
                        this.StopMoving();
                        this.GotoState<Active>();
                        this.Monitor<LivenessMonitor>(new LivenessMonitor.IdleEvent());
                    }
                }

                this.SendEvent(this.CreatorId, new NavigatorResetEvent());
            }

            return default;
        }

        private void DispatchTimer(Event e)
        {
            if (e is TimerElapsedEvent tee)
            {
                this.SendEvent(this.Id, this.TimerSpecificEvents[(string)tee.Info.Payload]);
            }
        }

        [OnEntry(nameof(OnInitActive))]
        [OnEventGotoState(typeof(Navigator.DrinkOrderProducedEvent), typeof(ExecutingOrder))]
        [OnEventDoAction(typeof(Navigator.DrinkOrderConfirmedEvent), nameof(OnDrinkOrderConfirmed))]
        internal class Active : State { }

        private void OnInitActive()
        {
            this.SendEvent(this.NavigatorId, new Navigator.GetDrinkOrderEvent(this.GetPicture()));
            this.Monitor<LivenessMonitor>(new LivenessMonitor.BusyEvent());
            this.WriteLine("<Robot> Asked for a new Drink Order");
        }

        private void OnDrinkOrderConfirmed()
        {
            // this.DrinkOrderConfirmed = true; // this is where it really belongs.
            this.SendEvent(this.CreatorId, new RobotReadyEvent());
        }

        public RoomPicture GetPicture()
        {
            var now = DateTime.UtcNow;
            this.WriteLine($"<Robot> Obtained a Room Picture at {now} UTC");
            return new RoomPicture() { TimeTaken = now, Image = ReadCamera() };
        }

        private static byte[] ReadCamera()
        {
            return new byte[1]; // todo: plug in real camera code here.
        }

        [OnEntry(nameof(OnInitExecutingOrder))]
        [OnEventGotoState(typeof(DrivingInstructionsEvent), typeof(ReachingClient))]
        internal class ExecutingOrder : State { }

        private void OnInitExecutingOrder(Event e)
        {
            this.CurrentOrder = (e as Navigator.DrinkOrderProducedEvent)?.DrinkOrder;

            if (this.CurrentOrder != null)
            {
                this.WriteLine("<Robot> Received new Drink Order. Executing ...");
                this.ExecuteOrder();
            }
        }

        private void ExecuteOrder()
        {
            var clientLocation = this.CurrentOrder.ClientDetails.Coordinates;
            this.WriteLine($"<Robot> Asked for driving instructions from {this.Coordinates} to {clientLocation}");

            this.SendEvent(this.NavigatorId, new Navigator.GetDrivingInstructionsEvent(this.Coordinates, clientLocation));
            this.Monitor<LivenessMonitor>(new LivenessMonitor.BusyEvent());
        }

        [OnEntry(nameof(ReachClient))]
        internal class ReachingClient : State { }

        private Transition ReachClient(Event e)
        {
            var route = (e as DrivingInstructionsEvent)?.Route;
            if (route != null)
            {
                this.Route = route;
                this.DrinkOrderConfirmed = false;
                this.Timers["MoveTimer"] = this.StartTimer(TimeSpan.FromSeconds(MoveDuration), "MoveTimer");
            }

            return this.GotoState<MovingOnRoute>();
        }

        [OnEventDoAction(typeof(MoveTimerElapsedEvent), nameof(NextMove))]
        [IgnoreEvents(typeof(Navigator.DrinkOrderProducedEvent))]
        internal class MovingOnRoute : State { }

        private Transition NextMove()
        {
            Transition nextState;

            if (this.Route == null)
            {
                return default;
            }

            if (!this.Route.Any())
            {
                this.StopMoving();
                nextState = this.GotoState<ServingClient>();

                this.WriteLine("<Robot> Reached Client.");
                Specification.Assert(
                    this.Coordinates == this.CurrentOrder.ClientDetails.Coordinates,
                    "Having reached the Client the Robot's coordinates must be the same as the Client's, but they aren't");
            }
            else
            {
                var nextDestination = this.Route[0];
                this.Route.RemoveAt(0);
                this.MoveTo(nextDestination);

                this.Timers["MoveTimer"] = this.StartTimer(TimeSpan.FromSeconds(MoveDuration), "MoveTimer");
                nextState = default;
            }

            return nextState;
        }

        private void StopMoving()
        {
            this.Route = null;
            this.DestroyTimer("MoveTimer");
        }

        private void DestroyTimer(string name)
        {
            if (this.Timers[name] != null)
            {
                this.StopTimer(this.Timers[name]);
                this.Timers[name] = null;
            }
        }

        private void MoveTo(Location there)
        {
            this.WriteLine($"<Robot> Moving from {this.Coordinates} to {there}");
            this.Coordinates = there;
        }

        [OnEntry(nameof(ServeClient))]
        internal class ServingClient : State { }

        private Transition ServeClient()
        {
            this.WriteLine("<Robot> Serving order");
            var drinkType = this.SelectDrink();
            var glassOfDrink = this.GetFullFlass(drinkType);

            return this.FinishOrder();
        }

        private Transition FinishOrder()
        {
            this.WriteLine("<Robot> Finished serving the order. Retreating.");
            this.WriteLine("==================================================");
            this.WriteLine(string.Empty);
            this.MoveTo(StartingLocation);
            this.CurrentOrder = null;
            this.DrinkOrderConfirmed = true;
            this.Monitor<LivenessMonitor>(new LivenessMonitor.IdleEvent());
            return this.RunForever ? this.GotoState<Active>() : this.GotoState<FinishState>();
        }

        private DrinkType SelectDrink()
        {
            var clientType = this.CurrentOrder.ClientDetails.PersonType;
            var selectedDrink = this.GetRandomDrink(clientType);
            this.WriteLine($"<Robot> Selected \"{selectedDrink}\" for {clientType} client");
            return selectedDrink;
        }

        private Glass GetFullFlass(DrinkType drinkType)
        {
            var fillLevel = 100;
            this.WriteLine($"<Robot> Filled a new glass of {drinkType} to {fillLevel}% level");
            return new Glass(drinkType, fillLevel);
        }

        private DrinkType GetRandomDrink(PersonType drinkerType)
        {
            var appropriateDrinks = drinkerType == PersonType.Adult
                ? Drinks.ForAdults
                : Drinks.ForMinors;
            return appropriateDrinks[this.RandomInteger(appropriateDrinks.Count)];
        }

        [OnEntry(nameof(Finish))]
        internal class FinishState : State { }

        private void Finish()
        {
            this.SendEvent(this.CreatorId, new CompletedEvent());
            this.Monitor<LivenessMonitor>(new LivenessMonitor.IdleEvent());
            this.SendEvent(this.Id, HaltEvent.Instance);
        }

        protected override Task OnEventUnhandledAsync(Event e, string state)
        {
            // this can be handy for debugging.
            return base.OnEventUnhandledAsync(e, state);
        }

        private void WriteLine(string s, params object[] args)
        {
            string msg = string.Format(s, args);
            Console.WriteLine(msg);
            // this ensures all our logging shows up in the coyote test trace which is handy!
            this.Logger.WriteLine(msg);
        }
    }
}