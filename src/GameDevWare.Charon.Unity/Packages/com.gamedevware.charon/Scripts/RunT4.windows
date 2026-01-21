@echo off
setlocal

:: Find current directory of the script
set "SCRIPT_DIR=%~dp0"

:: Check if dotnet is installed
:Check_Dotnet

where dotnet >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    goto Exit_No_Dotnet_Installed
)

:: Get the installed dotnet version
for /f "tokens=*" %%i in ('dotnet --version') do set "DOTNET_VERSION=%%i"

:: Extract the major version number
for /f "tokens=1 delims=." %%a in ("%DOTNET_VERSION%") do set "MAJOR_VERSION=%%a"

:: Check if the major version is 8 or later
if %MAJOR_VERSION% LSS 8 (
    goto Old_Dotnet_Installed
)

:: Install/Update t4 tool
:Install_Update_T4_Tool

pushd "%SCRIPT_DIR%"
IF NOT EXIST ".config\dotnet-tools.json" IF NOT EXIST "dotnet-tools.json" (
    dotnet new tool-manifest -o . >nul
)

:: Fix .NET SDK 10 behaviour with creating manifest in current directory
if EXIST "dotnet-tools.json" (
	:: Ensure the .config directory exists
	if NOT EXIST ".config" mkdir .config
	
	:: Move the file to the expected location
	move /Y "dotnet-tools.json" ".config\dotnet-tools.json" >nul
)

dotnet tool list --local | findstr /i /c:"dotnet-t4" >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    dotnet tool update dotnet-t4 --local --tool-manifest .config/dotnet-tools.json >nul
) else (
    dotnet tool install dotnet-t4 --local --tool-manifest .config/dotnet-tools.json >nul

    if %ERRORLEVEL% NEQ 0 (
        popd
        goto Exit_Failure_Dotnet_Restore_Failed
    )
)
popd

:: Run charon tool with specified parameters
:Run_Tool

pushd "%SCRIPT_DIR%"
dotnet t4 %*
set EXITCODE=%ERRORLEVEL%
popd

if %EXITCODE% NEQ 0 (
    goto Exit_Failure
) else (
    goto Exit_Success
)

goto Exit_Success

:Exit_Failure_Dotnet_Restore_Failed
set EXITCODE=-2
echo Failed to execute the 'dotnet tool install dotnet-t4' command to retrieve the latest package version from NuGet. Ensure that the 'dotnet' tool is installed and available in the 'PATH'. Check 'https://dotnet.microsoft.com/en-us/download' for the installer. 1>&2
goto Exit_Failure

:Exit_No_Dotnet_Installed
set EXITCODE=-3
echo .NET SDK 8+ is not installed. 1>&2
echo Please install .NET SDK from https://dotnet.microsoft.com/en-us/download 1>&2
goto Exit_Failure

:Old_Dotnet_Installed
set EXITCODE=-4
echo .NET SDK version %DOTNET_VERSION% is installed, while 8 or later is required. 1>&2
echo Please install .NET SDK from https://dotnet.microsoft.com/en-us/download 1>&2
goto Exit_Failure

:Exit_Failure
exit /B %EXITCODE%

:Exit_Success
exit /B 0
