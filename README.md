# Coyote Samples

[![NuGet](https://img.shields.io/nuget/v/Microsoft.Coyote.svg)](https://www.nuget.org/packages/Microsoft.Coyote/)
![Windows CI](https://github.com/microsoft/coyote-samples/workflows/Windows%20CI/badge.svg)
![Linux CI](https://github.com/microsoft/coyote-samples/workflows/Linux%20CI/badge.svg)
![macOS CI](https://github.com/microsoft/coyote-samples/workflows/macOS%20CI/badge.svg)
[![Join the chat at https://gitter.im/Microsoft/coyote](https://badges.gitter.im/Microsoft/coyote.svg)](https://gitter.im/Microsoft/coyote?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

This repository contains two sets of [Coyote](https://github.com/microsoft/coyote) samples.

The first set of samples shows how you can use Coyote to systematically test unmodified C#
task-based applications and services:

- [AccountManager](./AccountManager): demonstrates how to write a simple task-based C# application
  to create, get and delete account records in a backend NoSQL database and then systematically test
  this application using Coyote to find a race condition.
- [BoundedBuffer](./BoundedBuffer): demonstrates how to use `coyote rewrite` to find deadlocks in
  unmodified C# code.
- [Coffee Machine Failover (Tasks)](./CoffeeMachineTasks): demonstrates how to systematically test
  the failover logic in your task-based applications.
- [ImageGalleryAspNet](./ImageGalleryAspNet): demonstrates how to use Coyote to test an ASP.NET Core
  service.

The second set of samples shows how you can use the Coyote
[actor](https://microsoft.github.io/coyote/advanced-topics/actors/overview/) programming model
to build reliable applications and services:

- [HelloWorldActors](./HelloWorldActors): demonstrates how to write a simple Coyote application
  using actors, and then run and systematically test it.
- [Coffee Machine Failover (Actors)](./CoffeeMachineActors): demonstrates how to systematically test
  the failover logic in your Coyote actor applications.
- [Robot Navigator Failover (Actors)](./DrinksServingRobotActors): demonstrates how to
  systematically test the failover logic in your Coyote actors applications.
- [CloudMessaging](./CloudMessaging): demonstrates how to write a Coyote application that contains
  components that communicate with each other using the [Azure Service
  Bus](https://azure.microsoft.com/en-us/services/service-bus/) cloud messaging queue.
- [Timers in Actors](./Timers): demonstrates how to use the timer API of the Coyote actor
  programming model.

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
