cd %~dp0
dotnet ..\..\Coyote\bin\net5.0\coyote.dll test ../bin/net5.0/Raft.Nondeterminism.dll -i 1000 -ms 500 -graph-bug
