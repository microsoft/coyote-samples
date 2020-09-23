// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Text;

namespace ImageGallery.Logging
{
    /// <summary>
    /// Simple logger that writes text to the console.
    /// </summary>
    internal sealed class MockLogger : TextWriter
    {
        private static readonly object ColorLock = new object();

        public override Encoding Encoding => Console.OutputEncoding;

        public override void Write(char value)
        {
            Console.Write($"{GetRequestId()}{value}");
        }

        public override void Write(string value)
        {
            Console.Write($"{GetRequestId()}{value}");
        }

        public override void WriteLine(string value)
        {
            Console.WriteLine($"{GetRequestId()}{value}");
        }

        public void WriteErrorLine(string value)
        {
            lock (ColorLock)
            {
                try
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    var requestId = RequestId.Get();
                    Console.WriteLine($"{GetRequestId()}{value}");
                }
                finally
                {
                    Console.ResetColor();
                }
            }
        }

        public void WriteWarningLine(string value)
        {
            lock (ColorLock)
            {
                try
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"{GetRequestId()}{value}");
                }
                finally
                {
                    Console.ResetColor();
                }
            }
        }

        private static string GetRequestId()
        {
            var requestId = RequestId.Get();
            return requestId == Guid.Empty ? string.Empty : $"[{requestId}] ";
        }
    }
}