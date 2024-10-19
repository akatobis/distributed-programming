@echo off
start cmd /c "taskkill /f /im valuator.exe"
start cmd /c "taskkill /f /im nginx.exe"
start cmd /c "taskkill /f /im RankCalculator.exe"
start cmd /c "taskkill /f /im nats-server.exe"
start cmd /c "taskkill /f /im EventLogger.exe"