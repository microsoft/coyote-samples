# Coyote Examples
A collection of examples that show how to use Coyote to build and
systematically test asynchronous applications.

There are two sets of examples using the two main Coyote programming models:

- [AsyncTasks](AsyncTasks/README.md), which show how to use the Coyote `asynchronous tasks` programming model.
- [StateMachines](StateMachines/README.md), which show how to use the Coyote `state machines` programming model.

## How to build
To build the examples, run the following powershell script (or manually build the solutions
inside each directory):
```
powershell -f .\build.ps1
```
