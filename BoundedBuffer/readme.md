# Extreme Programming meets Systematic Testing using Coyote

by Immad Naseer, Chris Lovett and Pantazis Deligiannis

Our world is increasingly reliant on cloud and software services and these services are expected to
have high availability and handle high throughput with minimal latency. They are expected to be
resilient and reliable in the presence of internal and external failures and maintain a high
quality bar in the presence of continuous evolution and change. This is a tall ask and bugs can
fall through the cracks despite rigorous testing and good intentions.

Running unit tests and integration tests on every check-in is standard industry practice. We even
stress test our services to ensure reliability and performance. Subtle bugs still slip through. Why
is that?

It's important to realize the source of the complexity if we have any chance of taming it. Software
services are inherently concurrent in nature as their REST APIs can be called concurrently with
each other and they must be prepared to handle any interleaving of those calls. They are often
distributed across a fleet of machines which makes dealing with concurrency more challenging as
they can't readily use primitives likes locks and semaphores. Writing _correct_ concurrent code has
always been hard - the combinatorics of the state space are, so to say, exponential. We desperately
need new tools in our arsenal to help us tame this complexity. Coyote is one such tool which we'll
highlight in this article by applying it to a delightfully interesting example from the [annals of
the c2 wiki](http://wiki.c2.com/?ExtremeProgrammingChallenge).

Tom Cargill posed a [challenge](http://wiki.c2.com/?ExtremeProgrammingChallengeFourteen) to the
Extreme Programming community saying:

_Concurrent programs are hard to test, because of the combinatorial explosion in the state space
that must be covered. The state space explodes because of arbitrary context switching by a
scheduler. In general, it's impossible to use external inputs to force the program through the
states that must be covered, because a conventional test harness has no mechanism for influencing
the scheduler._

Well, it turns out this is **exactly** what [Coyote](https://microsoft.github.io/coyote) was
designed to solve. In this article we will show how Coyote can easily solve the programming problem
posed by Tom Cargill. He shared a BoundedBuffer implementation written in Java with a known but
tricky deadlock bug. Coyote works on .NET code, so the following is a C# version of the same
example using the .NET `System.Threading.Monitor` which contains the same bug:

```csharp
using System.Threading;

class BoundedBuffer
{
    public void Put(object x)
    {
        lock (this.syncObject)
        {
            while (this.occupied == this.buffer.Length)
            {
                Monitor.Wait(this.syncObject);
            }

            ++this.occupied;
            this.putAt %= this.buffer.Length;
            this.buffer[this.putAt++] = x;

            System.Threading.Monitor.Pulse(this.syncObject);
        }
    }

    public object Take()
    {
        object result = null;
        lock (this.syncObject)
        {
            while (this.occupied == 0)
            {
                Monitor.Wait(this.syncObject);
            }

            --this.occupied;
            this.takeAt %= this.buffer.Length;
            result = this.buffer[this.takeAt++];

            Monitor.Pulse(this.syncObject);
        }

        return result;
    }

    private readonly object syncObject = new object();
    private readonly object[] buffer = new object[4];
    private int putAt;
    private int takeAt;
    private int occupied;
}
```

The `BoundedBuffer` implements a buffer of fixed length with concurrent writers adding items to the
buffer and readers consuming items from the buffer. The readers wait if there are no items in the
buffer and writers wait if the buffer is full, resuming only once a slot has been consumed by a
reader. This is also known as a [producer/consumer
queue](https://en.wikipedia.org/wiki/Producer%E2%80%93consumer_problem).

The concrete ask was for the community to find the particular bug Cargill knew about in the above
program and the meta-ask was to come up with a methodology for catching such bugs rapidly.

At this point, it'll be worthwhile for you to take a few moments and reason through the above
program to see if you can spot the bug. It might also be helpful to read the discussion at this
[challenge's c2 wiki page](http://wiki.c2.com/?ExtremeProgrammingChallengeFourteen).

Can you spot the bug? Think hard.

If you're having difficulty seeing the bug, you're not alone as almost no one on the c2 wiki thread
was able to spot the bug. Rigorous testing on the JVM did not reliably reproduce the bug either
which lead to some skepticism that there even was a bug in the above implementation. It's a tricky
bug to find because when you add any kind of console debugging statements to debug it, the bug goes
away. So it's a classic [Heisenbug](https://en.wikipedia.org/wiki/Heisenbug).

We decided to apply [Coyote](https://microsoft.github.io/coyote) to the above challenge. Coyote
systematically controls and explores the concurrency and non-determinism encoded in programs and is
a perfect fit for a challenge like this. Coyote provides a drop-in replacement for
`System.Threading.Monitor` called
[SynchronizedBlock](http://microsoft.github.io/coyote/learn/programming-models/async/synchronized-block)
which allows Coyote to precisely control all the concurrency in this program so it can
systematically explore all the possibilities.

The following is an implementation of the above BoundedBuffer implementation using Coyote.

```csharp
using Microsoft.Coyote.Tasks;

public class BoundedBuffer
{
    public void Put(object x)
    {
        using (var monitor = SynchronizedBlock.Lock(this.syncObject))
        {
            while (this.occupied == this.buffer.Length)
            {
                monitor.Wait();
            }

            ++this.occupied;
            this.putAt %= this.buffer.Length;
            this.buffer[this.putAt++] = x;
            monitor.Pulse();
        }
    }

    public object Take()
    {
        object result = null;
        using (var monitor = SynchronizedBlock.Lock(this.syncObject))
        {
            while (this.occupied == 0)
            {
                monitor.Wait();
            }

            --this.occupied;
            this.takeAt %= this.buffer.Length;
            result = this.buffer[this.takeAt++];
            monitor.Pulse();
        }
        return result;
    }

    private readonly object syncObject = new object();
    private readonly object[] buffer = new object[4];
    private int putAt;
    private int takeAt;
    private int occupied;
}
```

The above is a very straightforward translation of the original code in C# leveraging Coyote's
`SynchronizedBlock`. We'll now have to write a small test driver program which Coyote can use to
find the bug.

The first test you write might look like this:

```csharp
[Microsoft.Coyote.SystematicTesting.Test]
public static void TestBoundedBufferWithDeadlock()
{
    BoundedBuffer buffer = new BoundedBuffer();
    var tasks = new List<Task>
    {
        Task.Run(() => Reader(buffer, 10)),
        Task.Run(() => Writer(buffer, 10))
    };

    Task.WaitAll(tasks.ToArray());
}
```

Here we setup 2 tasks, one is a reader calling `Take`, and the other is a Writer calling `Put`. The
following is the implementation of the test `Reader` and `Writer` methods:

```csharp
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
```

Clearly we have to `Put` the same number of items as we `Take` otherwise there will be a trivial
deadlock waiting for more items. We have matched both in this test with 10 iterations of each `Put`
and `Take`. We find no deadlock when we run the test above despite Coyote systematically exploring
different possible interleavings between the `Put` and `Take` calls.

This bug might be a bit more challenging to find. Let's think about this for a second - we have the
following variables at play here:

1. buffer size
2. number of concurrent readers
3. number of concurrent writers
4. number of iterations inside each task

The bug might only trigger in certain configurations but not in all configurations. Can we use
Coyote to explore the state space of the various configurations, _in addition_ to the state space
of the interleavings which result in each configuration?

Luckily, we can.

We can generate a random number of readers, writers, buffer length and iterations and let Coyote
explore various such configurations in addition to the interleavings in each configuration. The
following slightly more interesting Coyote test explores such configurations letting Coyote control
the non-determinism introduced by these random variables and the scheduling of the resulting number
of tasks:

```csharp

[Microsoft.Coyote.SystematicTesting.Test]
public static void TestBoundedBufferFindConfiguration(ICoyoteRuntime runtime)
{
    var random = Microsoft.Coyote.Random.Generator.Create();

    // Generate random configuration while ensuring we don't explore configurations
    // where the program trivially deadlocks (such as zero readers, zero writers,
    // mismatch between total produced and consumed items etc)
    int bufferSize = random.NextInteger(4) + 1;
    int readers = random.NextInteger(4) + 1;
    int writers = random.NextInteger(4) + 1;
    int iterations = random.NextInteger(10) + 1;

    int totalIterations = iterations * readers;
    int writerIterations = totalIterations / writers;
    int remainder = totalIterations % writers;

    runtime.Logger.WriteLine("Testing buffer size {0}, reader={1}, writer={2}, iterations={3}",
        bufferSize, readers, writers, iterations);

    BoundedBuffer buffer = new BoundedBuffer(bufferSize);
    var tasks = new List<Task>();
    int taskId = 0;
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

    Task.WaitAll(tasks.ToArray());
}
```

We can now test this using the `coyote test` tool see if Coyote can find the magic test
configuration that creates a deadlock.

```
coyote test BoundedBuffer.dll --iterations 100
    -m BoundedBufferExample.Program.TestBoundedBufferFindDeadlockConfiguration
```

Which outputs the following:

```
Starting TestingProcessScheduler in process 34704
... Created '1' testing task.
... Task 0 is using 'random' strategy (seed:3652188098).
..... Iteration #1
..... Iteration #2
..... Iteration #3
..... Iteration #4
..... Iteration #5
..... Iteration #6
..... Iteration #7
..... Iteration #8
..... Iteration #9
..... Iteration #10
..... Iteration #20
... Task 0 found a bug.
... Emitting task 0 traces:
..... Writing .\bin\Debug\netcoreapp3.1\CoyoteOutput\BoundedBuffer_0_7.txt
..... Writing .\bin\Debug\netcoreapp3.1\CoyoteOutput\BoundedBuffer_0_7.schedule
... Elapsed 10.5390128 sec.
... Testing statistics:
..... Found 1 bug.
... Scheduling statistics:
..... Explored 22 schedules: 22 fair and 0 unfair.
..... Found 4.55% buggy schedules.
..... Number of scheduling points in fair terminating schedules: 26 (min), 257 (avg), 535 (max).
... Elapsed 10.6406685 sec.
. Done

```

So here we see it quickly (in 10 seconds!) found the bug after iteration 20 and the log file
contains the telltale message:

```xml
<ErrorLog> Deadlock detected. Task(0) is waiting for a task to complete, but no other
controlled tasks are enabled. Task(4), Task(5), Task(6), Task(7), Task(8), Task(9), Task(11),
Task(12), Task(16) and Task(17) are waiting to acquire a resource that is already acquired, but no
other controlled tasks are enabled.
```

You will also see in the log that our own `WriteLine` shows us the configuration that failed:

```
Testing buffer size 1, reader=5, writer=4, iterations=6
```

We can see it took a total of 9 concurrent tasks (5 readers and 4 writers on buffer of size 1) to
generate a deadlock. Before we take a deeper look at this deadlock, let's see if there is a smaller
configuration which can also reproduce the bug. To help find this minimal test, `coyote test` has a
handy option called `--explore` which tells Coyote to keep on testing for all the given iterations
and report all bugs found which we can do like this:

```
coyote test BoundedBuffer.dll --iterations 1000 --explore --verbose
    -m BoundedBufferExample.Program.TestBoundedBufferFindDeadlockConfiguration > log.txt
```

This found 70 test configurations that failed.  We want the minimal test, so we can filter this
log.txt file to print only those configurations with 1 writer, and the result is:

```
Testing buffer size 1, reader=2, writer=1, iterations=10
Testing buffer size 1, reader=4, writer=1, iterations=8
Testing buffer size 1, reader=2, writer=1, iterations=2
Testing buffer size 1, reader=5, writer=1, iterations=10
Testing buffer size 1, reader=3, writer=1, iterations=9
Testing buffer size 1, reader=4, writer=1, iterations=8
Testing buffer size 1, reader=2, writer=1, iterations=7
Testing buffer size 1, reader=2, writer=1, iterations=2
Testing buffer size 1, reader=2, writer=1, iterations=8
Testing buffer size 1, reader=2, writer=1, iterations=10
Testing buffer size 1, reader=4, writer=1, iterations=10
Testing buffer size 1, reader=4, writer=1, iterations=6
Testing buffer size 1, reader=5, writer=1, iterations=5
Testing buffer size 1, reader=4, writer=1, iterations=9
Testing buffer size 1, reader=5, writer=1, iterations=7
Testing buffer size 1, reader=5, writer=1, iterations=2
Testing buffer size 1, reader=2, writer=1, iterations=7
Testing buffer size 1, reader=3, writer=1, iterations=6
Testing buffer size 1, reader=5, writer=1, iterations=8
Testing buffer size 1, reader=4, writer=1, iterations=7
```

So indeed, we now see clearly here that there is a minimal test with 2 readers and 1 writer. We
also see all these deadlocks can be found with a buffer size of 1 and a small number of iterations.
Now we can write the minimal test. We'll use 10 iterations just to be sure it deadlocks often:

```
[Microsoft.Coyote.SystematicTesting.Test]
public static void TestBoundedBufferMinimalDeadlock()
{
    BoundedBuffer buffer = new BoundedBuffer(1);
    var tasks = new List<Task>() {
        Task.Run(() => Reader(buffer, 5)),
        Task.Run(() => Reader(buffer, 5)),
        Task.Run(() => Writer(buffer, 10))
    };

    Task.WaitAll(tasks.ToArray());
}
```

And indeed, when we run this outside of Coyote it deadlocks, almost every time, which is great. But
when we add `Console.WriteLines` inside the BoundedBuffer implementation the deadlock no longer
occurs, due to its Heisenbug nature. `Console.WriteLine` somehow changes the timing just enough
that the deadlock no longer occurs.

Fortunately when we run this using `coyote test` using
`-m BoundedBufferExample.Program.TestBoundedBufferMinimalDeadlock` another log file is produced called
`BoundedBuffer_0_0.schedule`. This is a magic file that Coyote can use to replay the bug using:

```
coyote replay BoundedBuffer.dll BoundedBuffer_0_0.schedule -m BoundedBufferExample.Program.TestBoundedBufferMinimalDeadlock
```

With this you can step through your program in the debugger, take as long as you want, and the bug
will always be found. This is a HUGE advantage to anyone debugging these kinds of Heisenbugs.

## Explaining the bug

Now that Coyote has found the bug we can dive in and explore what is really happening. We added
detailed logging in both Take and Put methods and replayed the buggy trace using Coyote's replay
feature and ended up with the following graph. The horizontal axis represents time and the vertical
axis represents various steps inside the `Take` and `Put` methods (both Take and Put methods have a
similar structure so they share the same vertical axis).

![chart](chart.png)

The program starts out with the two `Reader` tasks attempting to take an item from the buffer. The
buffer is empty at this point so both of them go to the wait state till they are woken up by a
`Pulse` signal. The sole `Writer` task starts next, puts an item in the buffer and sends a `Pulse`
signal which awakens the first task that entered the wait queue. Before the `Reader` 1 task can
start running however, the `Writer` task is able to re-acquire the lock and starts a second run.
This can happen due to non-determinism in your operating system thread scheduling and is a crucial
step in creating this bug. The buffer is already full so the `Writer` task goes into the wait
state. `Reader` 1 finally gets its chance to run, consumes the item in the buffer and sends a
`Pulse` signal to wake up the next task in the wait queue. Since `Reader` 2 is at the head of
queue, it runs next but immediately blocks as the buffer is still empty. At this point, both the
`Writer` task and `Reader` 2 task are blocked. `Reader` 1 task runs again but goes into the wait
state as the buffer is empty. At this point, all three tasks are in the waiting state and so the
application is deadlocked. This deadlock requires a number of iterations of the readers and writers
and a random scheduling decision by the operating system in order to occur which is why it's
difficult to foresee and reliably reproduce.

Now given this understanding we can also get a hint at the right fix. One fix is to use `PulseAll`
which will wake up every task, and this indeed works. Another, perhaps more efficient fix is to
send out a `Pulse` right before calling `Wait`. This way the `Reader` 2 task sends another `Pulse`
and wakes the `Writer` task so things could proceed. You can also test these fixes using Coyote
with the full `TestBoundedBufferFindConfiguration` test function to make sure it doesn't find
another pesky test configuration.

## Lessons

Coyote was able to help us quickly find a way to trigger the subtle race condition which lead to
the deadlock and then boil it down to the simplest possible 100% reproducible trace. Making the
test smaller actually made it easier to find the bug. This can be counter-intuitive as developers
traditionally stress test their applications with a large number of requests to trigger such race
conditions and might still not be able to reliably reproduce them. Coyote on the other hand
systematically explores the state space so it can reliably reproduce bugs with much smaller tests.
In fact, the efficiency of Coyote at finding these bugs increases with the tests getting smaller.

The bug in the above implementation was not obvious. In fact, the authors had to spend some time
poring over Coyote's reproducible trace before finally grokking the bug. This speaks to the
effectiveness of tools like Coyote and the inability of humans to always foresee subtle race
conditions like the above.

While the `BoundedBuffer` implementation may seem _academic_, it's important to generalize the
lessons from this exercise and see how they can apply to your production services. Production
services contain way more than just two methods (Put and Take in this example) and expose a number
of REST APIs all of which are called in a highly concurrent manner and most of which operate on
shared resources (like the underlying buffer in the above example). Distributed services can't
always use concurrency control mechanisms like locks when coordinating work across independent
back-end services, nor can they use distributed transactions to coordinate data in, say, two
independent partitions of a scalable key-value store like CosmosDB. Furthermore, individual nodes
in a distributed service can crash at any point in time and have to gracefully deal with failure at
each possible step, while dealing with the external state constantly evolving and changing from
under them.

The above are hard problems. Appreciating the complexity and size of the state space is the first
step towards building more reliable software services. Tools like Coyote can help teams tame the
combinatorial complexity by systematically exploring the state space to catch safety and liveness
violations in every check-in and allow you to get the best of both worlds; extreme programming
without sacrificing quality. There is no tool that can magically find all bugs, so the best tool is
one which works with the developer, enabling them to follow their intuition and helping them design
the most efficient and maintainable tests and services. We think Coyote is indeed one such
tool.

The [complete code for this article](http://github.com/microsoft/coyote-samples/tree/master/BoundedBuffer)
is available on GitHub.