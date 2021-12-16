@ECHO OFF
SETLOCAL

SET PATH=.\.ci\tools\;%PATH%
SET CONFIGURATION=Debug

IF "%2"=="Release" (SET CONFIGURATION=Release)

powershell .\build\pack.ps1 -version %1 -configuration %CONFIGURATION%

ECHO "Telling TeamCity to publish the Alloy artifact even though the entire build isn't done"
ECHO ##teamcity[publishArtifacts '/artifacts/packages/Alloy.Sample.LabsBlockEnhancements*.nupkg']

EXIT /B %errorlevel%
