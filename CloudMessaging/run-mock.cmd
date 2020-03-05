dotnet %~dp0..\..\Coyote\bin\netcoreapp2.2\coyote.dll test %~dp0/../bin/netcoreapp2.2/Raft.Mocking.dll -i 1000 -ms 500 --timeout-delay 10 -graph-bug
