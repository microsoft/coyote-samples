// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.SystematicTesting;

namespace BoundedBufferExample
{
    public static class Program
    {
        public static void Main()
        {
            var runtime = RuntimeFactory.Create();
            var task = Task.Run(() => TestBoundedBufferMinimalDeadlock());
            Task.WaitAll(task);
            Console.WriteLine("Test complete - no deadlocks!");
        }

        [Microsoft.Coyote.SystematicTesting.Test]
        public static void TestBoundedBufferFindDeadlockConfiguration(ICoyoteRuntime runtime)
        {
            CheckRewritten();
            var random = Microsoft.Coyote.Random.Generator.Create();
            int bufferSize = random.NextInteger(5) + 1;
            int readers = random.NextInteger(5) + 1;
            int writers = random.NextInteger(5) + 1;
            int iterations = random.NextInteger(10) + 1;
            int totalIterations = iterations * readers;
            int writerIterations = totalIterations / writers;
            int remainder = totalIterations % writers;

            runtime.Logger.WriteLine("Testing buffer size {0}, reader={1}, writer={2}, iterations={3}", bufferSize, readers, writers, iterations);

            BoundedBuffer buffer = new BoundedBuffer(bufferSize);
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
        public static void TestBoundedBufferMinimalDeadlock()
        {
            CheckRewritten();
            BoundedBuffer buffer = new BoundedBuffer(1);
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

        [Microsoft.Coyote.SystematicTesting.Test]
        public static void TestBoundedBufferNoDeadlock()
        {
            CheckRewritten();
            BoundedBuffer buffer = new BoundedBuffer(1, true);
            var tasks = new List<Task>()
            {
                Task.Run(() => Reader(buffer, 5)),
                Task.Run(() => Reader(buffer, 5)),
                Task.Run(() => Writer(buffer, 10))
            };

            Task.WaitAll(tasks.ToArray());
        }

        private static void CheckRewritten()
        {
            if (!Microsoft.Coyote.Rewriting.AssemblyRewriter.IsAssemblyRewritten(typeof(Program).Assembly))
            {
                throw new Exception(string.Format("Error: please rewrite this assembly using coyote rewrite {0}", typeof(Program).Assembly.Location));
            }
        }
    }
}
