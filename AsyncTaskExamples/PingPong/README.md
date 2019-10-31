PingPong
========
This is a simple implementation of a ping-pong application in C# using the Coyote library and Async Programming Model with `ControlledTasks`.

A network environment machine (which is basically a test harness) creates a server and a client machine. The server and client machine then start exchanging ping and pong events for a number of turns.

This example is doing the same thing as the Explicit State Machine version
of PingPong that you will find in the `MachineExamples` folder.

## How to test

Assuming you have cloned and built the [Coyote](https://github.com/microsoft/coyote) github project you will have a tool named CoyoteTester.
Set an environment variable named `CoyoteBinaries` pointing to the
`bin` folder in this repo.

To test the produced executable use the following command:

Using .NET 4.6:
```
%Coyote%\net46\CoyoteTester -test:bin\net46\PingPong.exe -v -i:100
```

Using .NET Core 2.1:
```
dotnet %Coyote%\netcoreapp2.1\CoyoteTester.dll -test:bin\netcoreapp2.1\PingPong.dll -v -i:100
```

With the above command, the Coyote tester will systematically test the program for 100 testing iterations.

Note that this program is very simple and you might expect the Specification in the Client is obvious:

```C#
Specification.Assert(this.IsActive, "Client is not active");
```

However, when you run the CoyoteTester you will find that the test fails with this output.
The reason for this is there is a race condition between the time the client resets IsActive
and the time that the Server sends the last Pong event such that sometimes that last Pong
arrives too late causing the Specification.Assert to fire.  CoyoteTester can find this because
it tests different asynchronous schedules, which can simulate what might happen on a busy machine.
Timing issues like this are a common source of bugs in async code.  While C# makes it very easy to
use async/await, this trivial example show how important it is that you test your async code thoroughly.

```
Starting TestingProcessScheduler in process 38928
... Created '1' testing task.
... Task 0 is using 'Random' strategy (seed:954).
..... Iteration #1
<TestLog> Running test 'Coyote.Examples.PingPong.Program.Execute'.
Client is activated
Client sending Ping to Server
Client request: 1 / 5
Server received a ping, sending back a pong.
Client received a pong.
Client sending Ping to Server
Client request: 2 / 5
Server received a ping, sending back a pong.
Client received a pong.
Client sending Ping to Server
Client request: 3 / 5
Server received a ping, sending back a pong.
Client received a pong.
Client sending Ping to Server
Client request: 4 / 5
Server received a ping, sending back a pong.
Client received a pong.
Client sending Ping to Server
Client request: 5 / 5
Client halting
Client received a pong.
<ErrorLog> Client is not active
<StrategyLog> Found bug using 'Random' strategy.
Error: Client is not active
... Task 0 found a bug.
... Emitting task 0 traces:
..... Writing bin\netcoreapp2.1\Output\PingPong.dll\CoyoteTesterOutput\PingPong_0_1.pstrace
..... Writing bin\netcoreapp2.1\Output\PingPong.dll\CoyoteTesterOutput\PingPong_0_1.schedule
... Elapsed 0.097971 sec.
... ### Task 0 is terminating
... Testing statistics:
..... Found 1 bug.
... Scheduling statistics:
..... Explored 1 schedule: 1 fair and 0 unfair.
..... Found 100.00% buggy schedules.
..... Number of scheduling points in fair terminating schedules: 15 (min), 15 (avg), 15 (max).
... Elapsed 0.2114789 sec.
. Done
```