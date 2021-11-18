﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote;
using Microsoft.Coyote.SystematicTesting;

namespace PetImages.Tests
{
    public static class Program
    {
        public static void Main()
        {
            var tests = new Tests();
            var config = Configuration.Create()
                .WithDebugLoggingEnabled()
                .WithVerbosityEnabled();
            var engine = TestingEngine.Create(config, tests.TestFirstScenario);
            engine.Run();
            Console.WriteLine($"Bugs found: {engine.TestReport.NumOfFoundBugs}");
        }
    }
}
