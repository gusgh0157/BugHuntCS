@echo off
rem BugHunt grader: grade.bat [labs] [options]   e.g. grade.bat 83-88 --config Release   grade.bat C
rem See README.md, grader section.
dotnet run --project "%~dp0BugHunt.Grader" -- %*
