# Coyote Samples

[![Build status](https://dev.azure.com/foundry99/Coyote/_apis/build/status/Samples/Coyote-Samples-VS-2019-CI)](https://dev.azure.com/foundry99/Coyote/_build/latest?definitionId=100)

This repository contains a series of samples that highlight how to use
[Coyote](https://github.com/microsoft/coyote) for building reliable services and applications.

- [HelloWorldTasks](./HelloWorldTasks): demonstrates how to write a simple Coyote application using asynchronous tasks, and then run and systematically test it locally.
- [HelloWorldActors](./HelloWorldActors): demonstrates how to write a simple Coyote application using actors, and then run and systematically test it locally.
- [Coffee Machine Failover (Actors)](./CoffeeMachineActors): demonstrates how to systematically test the failover logic in your Coyote applications using actors.
- [Coffee Machine Failover (Tasks)](./CoffeeMachineTasks): demonstrates how to systematically test the failover logic in your Coyote applications using asynchronous tasks.
- [Robot Navigator Failover (Actors)](./DrinksServingRobotActors): demonstrates how to systematically test the failover logic in your Coyote applications using actors.
- [CloudMessaging](./CloudMessaging): demonstrates how to write a Coyote application that contains components that communication with each other using the [Azure Service Bus](https://azure.microsoft.com/en-us/services/service-bus/) cloud messaging framework.
- [Timers in Actors](./Timers): demonstrates how to use the timer API of the Coyote actor programming model.
- [BoundedBuffer](/.BoundedBuffer): demonstrates how to use `coyote rewrite` to find deadlocks in unmodified C# code.
- [ImageGalleryAspNet](/.ImageGalleryAspNet): demonstrates how to use coyote to test ASP.NET websites.

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
