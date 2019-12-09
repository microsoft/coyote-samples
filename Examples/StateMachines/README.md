Coyote State Machine Examples
=============================
A collection of examples that show how to use the Coyote to build and systematically test asynchronous in-memory applications.

The examples provided here show how to use the Coyote state machine programming model.

## Where to start
If you are new to Coyote, please check out the **PingPong** and **FailureDetector** examples which give an introduction to using basic and more advanced features of Coyote. If you have any questions, please get in touch!

## Examples
- **PingPong**, a simple application that consists of a client and a server sending ping and pong messages for a number of turns.
- **FailureDetector**, which demonstrates more advanced features of Coyote such as monitors (for specifying global safety and liveness properties) and nondeterministic timers which are controlled during testing.

## How to build
To build the examples, run the following powershell script (or manually build the `StateMachines.sln` solution):
```
powershell -f .\build-examples.ps1
```

## How to run
To execute a sample, simply run the corresponding executable, available in the `bin` folder.
