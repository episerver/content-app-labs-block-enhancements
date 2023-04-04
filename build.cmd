@ECHO OFF
SETLOCAL

SET PATH=.\.ci\tools\;%PATH%

REM Set Release or Debug configuration.
IF "%1"=="Release" (set CONFIGURATION=Release) ELSE (set CONFIGURATION=Debug)
ECHO Building in %CONFIGURATION%

REM Set the build version. Using defaults when no params are given (common when running locally).
IF "%2"=="" ( SET BUILD=01 ) ELSE ( SET BUILD=%2 )
IF "%3"=="" ( SET BRANCH=developerbuild ) ELSE ( SET BRANCH=%3 )

REM Build the C# solution.
set Logger="trx"

REM Build the C# solution.
powershell "%CD%\build\build.ps1" -configuration %Configuration% -logger %Logger%
IF %errorlevel% NEQ 0 EXIT /B %errorlevel%

EXIT /B %ERRORLEVEL%
