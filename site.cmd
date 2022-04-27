@ECHO OFF
IF "%1"=="" (set FRAMEWORK=net5.0) ELSE (set FRAMEWORK=%1)
ECHO Run site in %FRAMEWORK%

call SET ASPNETCORE_ENVIRONMENT=Development
call SET SolutionDir=%~dp0
call dotnet run --framework %FRAMEWORK% --project src\\Alloy.Sample\\Alloy.Sample.csproj
