// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;

namespace Microsoft.Coyote.Samples.Common
{
    internal class LogWriter
    {
        private static readonly ConsoleColor DefaultColor = Console.ForegroundColor;
        private readonly TextWriter Log;
        private readonly bool Echo;

        private LogWriter(TextWriter log, bool echo)
        {
            this.Log = log;
            this.Echo = echo;
        }

        public static LogWriter Instance;

        public static void Initialize(TextWriter log, bool echo)
        {
            Instance = new LogWriter(log, echo);
        }

        public void WriteLine(string format, params object[] args)
        {
            this.Log.WriteLine(format, args);
            if (this.Echo)
            {
                Console.WriteLine(format, args);
            }
        }

        public void WriteWarning(string format, params object[] args)
        {
            var msg = string.Format(format, args);
            Console.ForegroundColor = ConsoleColor.Yellow;
            try
            {
                this.Log.WriteLine(msg);
                if (this.Echo)
                {
                    Console.WriteLine(msg);
                }
            }
            finally
            {
                Console.ForegroundColor = DefaultColor;
            }
        }

        internal void WriteError(string format, params object[] args)
        {
            var msg = string.Format(format, args);
            Console.ForegroundColor = ConsoleColor.Red;
            try
            {
                this.Log.WriteLine(msg);
                if (this.Echo)
                {
                    Console.WriteLine(msg);
                }
            }
            finally
            {
                Console.ForegroundColor = DefaultColor;
            }
        }
    }
}
