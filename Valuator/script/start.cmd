@echo off
start cmd /c "D:\nats-server-2.10.14\nats-server.exe && nats-server"
start cmd /c "cd ..\RankCalculator && dotnet run"
start cmd /c "cd ..\Valuator && dotnet run --urls "http://0.0.0.0:5001""
start cmd /c "cd ..\Valuator && dotnet run --urls "http://0.0.0.0:5002""
start cmd /c "C:\nginx-1.25.4\nginx -c cd ..\nginx\conf\nginx.conf"