// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Tasks;

namespace BoundedBufferExample
{
    public static class Program
    {
        public static void Main()
        {
            var runtime = RuntimeFactory.Create();
            var task = Task.Run(() => TestBoundedBufferNoDeadlock(runtime));
            Task.WaitAll(task);
            Console.WriteLine("Test complete - no deadlocks!");
        }

        [Microsoft.Coyote.SystematicTesting.Test]
        public static void TestBoundedBufferFindDeadlockConfiguration(ICoyoteRuntime runtime)
        {
            var random = Microsoft.Coyote.Random.Generator.Create();
            int bufferSize = random.NextInteger(5) + 1;
            int readers = random.NextInteger(5) + 1;
            int writers = random.NextInteger(5) + 1;
            int iterations = random.NextInteger(10) + 1;
            int totalIterations = iterations * readers;
            int writerIterations = totalIterations / writers;
            int remainder = totalIterations % writers;

            runtime.Logger.WriteLine("Testing buffer size {0}, reader={1}, writer={2}, iterations={3}", bufferSize, readers, writers, iterations);

            BoundedBuffer buffer = new BoundedBuffer(bufferSize, runtime);
            var tasks = new List<Task>();
            for (int i = 0; i < readers; i++)
            {
                tasks.Add(Task.Run(() => Reader(buffer, iterations)));
            }

            int x = 0;
            for (int i = 0; i < writers; i++)
            {
                int w = writerIterations;
                if (i == writers - 1)
                {
                    w += remainder;
                }

                x += w;
                tasks.Add(Task.Run(() => Writer(buffer, w)));
            }

            Microsoft.Coyote.Specifications.Specification.Assert(x == totalIterations, "total writer iterations doesn't match!");

            Task.WaitAll(tasks.ToArray());
        }

        [Microsoft.Coyote.SystematicTesting.Test]
        public static void TestBoundedBufferMinimalDeadlock(ICoyoteRuntime runtime)
        {
            BoundedBuffer buffer = new BoundedBuffer(1, runtime);
            var tasks = new List<Task>()
            {
                Task.Run(() => Reader(buffer, 5)),
                Task.Run(() => Reader(buffer, 5)),
                Task.Run(() => Writer(buffer, 10))
            };

            Task.WaitAll(tasks.ToArray());
        }

        [Microsoft.Coyote.SystematicTesting.Test]
        public static void TestBoundedBufferNoDeadlock(ICoyoteRuntime runtime)
        {
            BoundedBuffer.BugFixed = true;
            BoundedBuffer buffer = new BoundedBuffer(1, runtime);
            var tasks = new List<Task>()
            {
                Task.Run(() => Reader(buffer, 5)),
                Task.Run(() => Reader(buffer, 5)),
                Task.Run(() => Writer(buffer, 10))
            };

            Task.WaitAll(tasks.ToArray());
        }

        private static void Reader(BoundedBuffer buffer, int iterations)
        {
            for (int i = 0; i < iterations; i++)
            {
                object x = buffer.Take();
            }
        }

        private static void Writer(BoundedBuffer buffer, int iterations)
        {
            for (int i = 0; i < iterations; i++)
            {
                buffer.Put("hello " + i);
            }
        }
    }
}
