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

        private string Value;

        private async ControlledTask WriteWithDelayAsync(string value)
        {
            await ControlledTask.Delay(100);
            this.Value = value;
        }

        public async ControlledTask RunAsync()
        {
            ControlledTask task1 = this.WriteWithDelayAsync(GoodMorning);
            ControlledTask task2 = this.WriteWithDelayAsync(HelloWorld);
            ControlledTask task3 = this.WriteWithDelayAsync(HelloWorld);

            await ControlledTask.WhenAll(task1, task2, task3);

            Console.WriteLine(this.Value);

            Specification.Assert(this.Value == HelloWorld, $"Value is '{this.Value}' instead of '{HelloWorld}'.");
        }
    }
}
