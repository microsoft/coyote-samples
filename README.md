# Coyote Samples

[![Build Status](https://dev.azure.com/foundry99/Coyote/_apis/build/status/CoyoteSamples/Coyote-Samples-CI?branchName=master)](https://dev.azure.com/foundry99/Coyote/_build/latest?definitionId=53&branchName=master)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)

This repository contains a series of samples that highlight how to use
[Coyote](https://github.com/microsoft/coyote) for building reliable services and applications.

- [HelloWorldTasks](./HelloWorldTasks): demonstrates how to write a simple Coyote application using asynchronous tasks, and then run and systematically test it locally.
- [HelloWorldActors](./HelloWorldActors): demonstrates how to write a simple Coyote application using actors, and then run and systematically test it locally.
- [HelloWorldStateMachines](./HelloWorldStateMachines): demonstrates how to write a simple Coyote application using state machines, and then run and systematically test it locally.
- [CloudMessaging](./CloudMessaging): demonstrates how to write a Coyote application that contains components that communication with each other using the [Azure Service Bus](https://azure.microsoft.com/en-us/services/service-bus/) cloud messaging framework.
This sample is made up of the following parts:
    - [Raft](./CloudMessaging/Raft) - a core C# class library that implements the [Raft Consensus Algorithm](https://raft.github.io/) using the Coyote [Actor Programming Model](https://microsoft.github.io/coyote/programming-models/actors/overview).
    - [Raft.Azure](./CloudMessaging/Raft.Azure) - a C# executable that shows how to run Coyote messages through an [Azure Service Bus](https://azure.microsoft.com/en-us/services/service-bus/).
    - [Raft.Mocking](./CloudMessaging/Raft.Mocking): demonstrates how to use mocks to systematically test in-memory the [CloudMessaging](./CloudMessaging) sample application.
    - [Raft.Nondeterminism](./CloudMessaging/Raft.Nondeterminism): demonstrates how to introduce controlled nondeterminism in your Coyote tests to systematically exercise corner-cases.
- [Timers in Actors](./Timers): demonstrates how to use the timer API of the Coyote actor programming model.
- [FailoverActors](./FailoverActors): demonstrates how to systematically test the failover logic in your Coyote actor applications.

To get started, clone this repository and build the samples by running the following PowerShell script:
```
.\build.ps1
```

Then, follow the instructions in each sample.

## Contributing
This project welcomes contributions and suggestions. Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repositories using our CLA.

## Code of Conduct
This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
