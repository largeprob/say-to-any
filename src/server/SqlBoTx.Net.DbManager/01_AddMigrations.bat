@echo off
REM 等待用户输入
set /p userInput=Please enter the file name for this migration:

REM 执行命令
dotnet ef  migrations  add  %userInput%  --json

REM 等待用户输入
echo Migration file created successfully

REM 暂停脚本
pause
