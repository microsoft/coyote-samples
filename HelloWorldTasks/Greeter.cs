// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;

namespace Microsoft.Coyote.Samples.HelloWorldTasks
{
    internal class Greeter
    {
        private const string HelloWorld = "Hello World!";
        private const string GoodMorning = "Good Morning";

        private string Value;

        private async Task WriteWithDelayAsync(string value)
        {
            await Task.Delay(100);
            this.Value = value;
        }

        public async Task RunAsync()
        {
            Task task2 = this.WriteWithDelayAsync(HelloWorld);
            Task task3 = this.WriteWithDelayAsync(HelloWorld);
            Task task1 = this.WriteWithDelayAsync(GoodMorning);
            Task task4 = this.WriteWithDelayAsync(HelloWorld);
            Task task5 = this.WriteWithDelayAsync(HelloWorld);

            await Task.WhenAll(task1, task2, task3, task4, task5);

            Console.WriteLine(this.Value);

            Specification.Assert(this.Value == HelloWorld, $"Value is '{this.Value}' instead of '{HelloWorld}'.");
        }
    }
}
