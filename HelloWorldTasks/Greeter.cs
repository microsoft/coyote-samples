// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Threading.Tasks;

namespace Microsoft.Coyote.Samples.HelloWorld
{
    internal class Greeter
    {
        private const string HelloWorld = "Hello World!";
        private const string GoodMorning = "Good Morning";

        internal class SharedEntry<T>
        {
            public T Value = default;
        }

        public static async ControlledTask WriteWithDelayAsync<T>(SharedEntry<T> entry, T value)
        {
            await ControlledTask.Delay(100);
            entry.Value = value;
        }

        public async ControlledTask RunAsync()
        {
            SharedEntry<string> entry = new SharedEntry<string>();

            ControlledTask task1 = WriteWithDelayAsync(entry, GoodMorning);
            ControlledTask task2 = WriteWithDelayAsync(entry, HelloWorld);
            ControlledTask task3 = WriteWithDelayAsync(entry, HelloWorld);

            await ControlledTask.WhenAll(task1, task2, task3);

            Console.WriteLine(entry.Value);

            Specification.Assert(entry.Value == HelloWorld, $"Value is '{entry.Value}' instead of '{HelloWorld}'.");
        }
    }
}
