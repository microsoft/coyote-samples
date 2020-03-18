cd %~dp0
dotnet ..\..\Coyote\bin\netcoreapp2.2\coyote.dll test /../bin/netcoreapp2.2/Raft.Mocking.dll -i 1000 -ms 500 -graph-bug
