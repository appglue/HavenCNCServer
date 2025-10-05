@echo off@echo off

REM Generate OpenAPI specification for HavenCNCServerREM Generate OpenAPI specification for HavenCNCServer

REM This script builds the project and extracts the OpenAPI definitionREM This script builds the project and extracts the OpenAPI definition



echo Generating OpenAPI specification for HavenCNCServer...echo Generating OpenAPI specification for HavenCNCServer...



REM Build the project firstREM Build the project first

echo Building project...echo Building project...

dotnet build --configuration Release --framework net8.0-windowsdotnet build --configuration Release



if %ERRORLEVEL% neq 0 (if %ERRORLEVEL% neq 0 (

    echo Build failed. Cannot generate OpenAPI spec.    echo Build failed. Cannot generate OpenAPI spec.

    exit /b 1    exit /b 1

))



REM Install swagger CLI tool if not already installedREM Install swagger CLI tool if not already installed

echo Checking for Swagger CLI tool...echo Checking for Swagger CLI tool...

dotnet tool list -g | findstr "swashbuckle.aspnetcore.cli" >nuldotnet tool list -g | findstr "swashbuckle.aspnetcore.cli" >nul



if %ERRORLEVEL% neq 0 (if %ERRORLEVEL% neq 0 (

    echo Installing Swagger CLI tool...    echo Installing Swagger CLI tool...

    dotnet tool install -g Swashbuckle.AspNetCore.CLI    dotnet tool install -g Swashbuckle.AspNetCore.CLI

))



set OUTPUT_FILE=openapi-batch.jsonREM Generate OpenAPI specification

echo Generating OpenAPI specification...

REM Create backup of existing file if it existsdotnet swagger tofile --output openapi.json bin/Release/net8.0-windows/HavenCNCServer.dll v1

if exist "%OUTPUT_FILE%" (

    for /f "tokens=1-6 delims=:/ " %%a in ('echo %date% %time%') do (if %ERRORLEVEL% equ 0 (

        set timestamp=%%c%%a%%b_%%d%%e%%f    echo OpenAPI specification generated successfully: openapi.json

    )    echo You can also view the interactive documentation by running the application and visiting: https://localhost:5001/swagger

    set timestamp=%timestamp: =0%) else (

    set timestamp=%timestamp:~0,15%    echo Failed to generate OpenAPI specification.

    set BACKUP_FILE=openapi-batch.backup.%timestamp%.json    exit /b 1

    echo Creating backup of existing file...)
    copy "%OUTPUT_FILE%" "%BACKUP_FILE%" >nul
    echo ✓ Backup created: %BACKUP_FILE%
)

REM Generate OpenAPI specification
echo Generating OpenAPI specification...
swagger tofile --output "%OUTPUT_FILE%" bin/Release/net8.0-windows/HavenCNCServer.dll v1

if %ERRORLEVEL% equ 0 (
    echo OpenAPI specification generated successfully: %OUTPUT_FILE%
    echo You can also view the interactive documentation by running the application and visiting: http://localhost:5000/swagger
) else (
    echo Failed to generate OpenAPI specification.
    exit /b 1
)