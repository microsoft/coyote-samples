cd %~dp0
dotnet ..\..\Coyote\bin\netcoreapp2.2\coyote.dll test ../bin/netcoreapp2.2/Raft.Nondeterminism.dll -i 1000 -ms 2000 -sch-pct 10 -graph-bug
