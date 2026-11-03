@echo off
rem BugHunt grader: grade.bat [labs] [options]   e.g. grade.bat 29-40 --config Release
rem See README.md, grader section.
dotnet run --project "%~dp0BugHunt.Grader" -- %*
