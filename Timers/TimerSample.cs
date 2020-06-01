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
    [OnEventDoAction(typeof(CustomTimerEvent), nameof(HandlePeriodicTimeout))]
    internal class TimerSample : Actor
    {
        /// <summary>
        /// Timer used in a periodic timer.
        /// </summary>
        private TimerInfo PeriodicTimer;

        /// <summary>
        /// A custom timer event
        /// </summary>
        internal class CustomTimerEvent : TimerElapsedEvent
        {
            /// <summary>
            /// Count of timeout events processed.
            /// </summary>
            internal int Count;
        }

        protected override Task OnInitializeAsync(Event initialEvent)
        {
            this.WriteMessage("<Client> Starting a non-periodic timer");
            this.StartTimer(TimeSpan.FromSeconds(1));
            return base.OnInitializeAsync(initialEvent);
        }

        private void HandleTimeout(Event e)
        {
            TimerElapsedEvent te = (TimerElapsedEvent)e;

            this.WriteMessage("<Client> Handling timeout from timer");

            this.WriteMessage("<Client> Starting a period timer");
            this.PeriodicTimer = this.StartPeriodicTimer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), new CustomTimerEvent());
        }

        private void HandlePeriodicTimeout(Event e)
        {
            this.WriteMessage("<Client> Handling timeout from periodic timer");
            if (e is CustomTimerEvent ce)
            {
                ce.Count++;
                if (ce.Count == 3)
                {
                    this.WriteMessage("<Client> Stopping the periodic timer");
                    this.WriteMessage("<Client> Press ENTER to terminate.");
                    this.StopTimer(this.PeriodicTimer);
                }
            }
        }

        private void WriteMessage(string msg, params object[] args)
        {
            // this little trick allows you to see our log messages in a different color when running
            // Timers.exe directly, just to make it easier to differentiate Coyote log messages from ours.
            Console.ForegroundColor = ConsoleColor.Yellow;
            try
            {
                this.Logger.WriteLine(msg, args);
            }
            finally
            {
                Console.ForegroundColor = DefaultColor;
            }
        }

        private static readonly ConsoleColor DefaultColor = Console.ForegroundColor;
    }
}
