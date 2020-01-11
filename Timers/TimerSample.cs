// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Actors.Timers;

namespace Coyote.Examples.Timers
{
    [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeout))]
    internal class TimerSample : Actor
    {
        /// <summary>
        /// Count of timeout events processed.
        /// </summary>
        private int Count;

        /// <summary>
        /// Timer used in a periodic timer.
        /// </summary>
        private TimerInfo PeriodicTimer;

        protected override Task OnInitializeAsync(Event initialEvent)
        {
            this.WriteMessage("<Client> Starting a non-periodic timer named 'Foo'");
            this.StartTimer(TimeSpan.FromSeconds(1), "Foo");
            return base.OnInitializeAsync(initialEvent);
        }

        private void HandleTimeout(Event e)
        {
            TimerElapsedEvent te = (TimerElapsedEvent)e;
            string label = te.Info.Payload.ToString();
            this.WriteMessage("<Client> Handling timeout from timer '{0}'", label);

            if (this.Count == 0)
            {
                this.WriteMessage("<Client> Starting a period timer named 'Bar'");
                this.PeriodicTimer = this.StartPeriodicTimer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), "Bar");
            }

            this.Count++;
            if (this.Count == 3)
            {
                this.WriteMessage("<Client> Stopping the periodic timer");
                this.StopTimer(this.PeriodicTimer);
            }
        }

        private void WriteMessage(string msg, params object[] args)
        {
            var saved = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            this.Logger.WriteLine(msg, args);
            Console.ForegroundColor = saved;
        }
    }
}
