@echo off

REM 执行命令
dotnet ef  migrations  remove      --json

REM 暂停脚本
pause
