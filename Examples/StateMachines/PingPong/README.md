PingPong
========
This is a simple implementation of a ping-pong application in C# using Coyote
with the state machine programming model.

A network environment machine (which is basically a test harness) creates a server and a client machine. The server and client machine then start
exchanging ping and pong events for a number of turns.

The aim of this sample is to show how to write basic Coyote programs using
the state machine programming model. You will find a similar example in the
`AsyncTasks` folder that uses the asynchronous tasks programming model.

We provide two different versions of the this program in `Examples\StateMachines`
as follows:

- This version uses simple Machine, Event and Send methods
- The `PingPong.AsyncAwait` version uses an async Receive method

## How to test

Assuming you have cloned and built the [Coyote](https://github.com/microsoft/coyote)
GitHub project you will have a tool named `coyote`.
Set an environment variable named `CoyoteBinaries` pointing to the
`bin` folder in this repo.

To test the produced executable use the following command:

Using .NET 4.6:
```
%CoyoteBinaries%\net46\coyote test bin\net46\PingPong.exe -v -i 100
```
Using .NET Core 2.1:
```
dotnet %CoyoteBinaries%\netcoreapp2.1\coyote.dll test bin\netcoreapp2.1\PingPong.dll -v -i 100
```

## Expected Results

With the above command, the Coyote tester will systematically test the program for 100 test iterations. Each iteration will explore slightly different paths through the state machine.
Note that this program is pretty simple and so there is an injected bug planted on purpose
just for `coyote` to find.

The `coyote` tool makes sure that all events are handled, which tells you that your state machine
is never receiving unexpected events for a given `MachineState`. For example, the Client has an
`Active` state that does expect a `Server.Pong` event. Coyote will make sure this event is
only received when the client is in the `Active` state.

Notice the `Terminating` state is not expecting a `Server.Pong` event because it does not declare
the `OnEventDoAction` attribute for it and so Coyote will ensure this is honored.

Can you find the bug? Hint: When the last Ping is sent the Client transitions
to the Terminating state.
But if it does this too quickly then it will not be in the active state when the Server sends back the
last Pong event. But since the Client immediately Halts when it enters the Terminating state it will
stop processing events before the Pong arrives and the program will exit cleanly. However, to turn
this shutdown condition into a bug, the server specifies `new SendOptions(mustHandle: true)` and since
the last Pong is not handled it becomes a test error and `coyote` outputs this error message
in the test trace output:

```
<ErrorLog> A must-handle event 'Coyote.Examples.PingPong.Server+Pong' was sent to the halted machine 'Coyote.Examples.PingPong.Client'.
```

While this may seem a contrived example, there are many applications where processing
all events is critical to the correctness of the program.
