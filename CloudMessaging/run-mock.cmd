cd %~dp0
dotnet ..\..\Coyote\bin\netcoreapp3.1\coyote.dll test /../bin/netcoreapp3.1/Raft.Mocking.dll -i 1000 -ms 500 -graph-bug
