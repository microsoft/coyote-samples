cd %~dp0
dotnet ..\packages\microsoft.coyote.test\1.4.1\lib\net5.0\coyote.dll test ../bin/net5.0/Raft.Nondeterminism.dll -i 1000 -ms 500 -graph-bug
