@echo off
start cmd /c "cd ..\nats && nats-server"
start cmd /c "cd ..\RankCalculator && dotnet run"
start cmd /c "cd ..\EventLogger && dotnet run"
start cmd /c "cd ..\EventLogger && dotnet run"
start cmd /c "cd ..\Valuator && dotnet run --urls "http://0.0.0.0:5001""
start cmd /c "cd ..\Valuator && dotnet run --urls "http://0.0.0.0:5002""
start cmd /c "cd ..\nginx && nginx"