# Generate OpenAPI specification for HavenCNCServer# Generate OpenAPI specification for HavenCNCServer

# This script offers multiple methods to generate the OpenAPI definition# This script offers multiple methods to generate the OpenAPI definition



param(param(

    [Parameter(HelpMessage="Method to use: 'runtime' (default) or 'cli'")]    [Parameter(HelpMessage="Method to use: 'runtime' (default) or 'cli'")]

    [ValidateSet("runtime", "cli")]    [ValidateSet("runtime", "cli")]

    [string]$Method = "runtime"    [string]$Method = "runtime"

))



Write-Host "Generating OpenAPI specification for HavenCNCServer using method: $Method" -ForegroundColor GreenWrite-Host "Generating OpenAPI specification for HavenCNCServer using method: $Method" -ForegroundColor Green



# Ensure we're in the project directory# Ensure we're in the project directory

Set-Location $PSScriptRootSet-Location $PSScriptRoot



if ($Method -eq "cli") {if ($Method -eq "cli") {

    # CLI Method using Swagger CLI tool    # CLI Method using Swagger CLI tool

    Write-Host "Using CLI method..." -ForegroundColor Yellow    Write-Host "Using CLI method..." -ForegroundColor Yellow

        

    # Build the project first    # Build the project first

    Write-Host "Building project..." -ForegroundColor Yellow    Write-Host "Building project..." -ForegroundColor Yellow

    dotnet build --configuration Release --framework net8.0-windows    dotnet build --configuration Release --framework net8.0-windows



    if ($LASTEXITCODE -ne 0) {    if ($LASTEXITCODE -ne 0) {

        Write-Host "Build failed. Cannot generate OpenAPI spec." -ForegroundColor Red        Write-Host "Build failed. Cannot generate OpenAPI spec." -ForegroundColor Red

        exit 1        exit 1

    }    }



    # Check if swagger CLI tool is installed    # Check if swagger CLI tool is installed

    $swaggerInstalled = dotnet tool list -g | Select-String "swashbuckle.aspnetcore.cli"    $swaggerInstalled = dotnet tool list -g | Select-String "swashbuckle.aspnetcore.cli"

    if (-not $swaggerInstalled) {    if (-not $swaggerInstalled) {

        Write-Host "Installing Swagger CLI tool..." -ForegroundColor Yellow        Write-Host "Installing Swagger CLI tool..." -ForegroundColor Yellow

        dotnet tool install -g Swashbuckle.AspNetCore.CLI        dotnet tool install -g Swashbuckle.AspNetCore.CLI

    }    }



    $outputFile = "openapi-cli.json"    # Generate OpenAPI specification using CLI

        Write-Host "Generating OpenAPI specification using CLI..." -ForegroundColor Yellow

    # Create backup of existing file if it exists    swagger tofile --output openapi-cli.json bin/Release/net8.0-windows/HavenCNCServer.dll v1

    if (Test-Path $outputFile) {

        $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"    if ($LASTEXITCODE -eq 0) {

        $backupFile = "openapi-cli.backup.$timestamp.json"        Write-Host "OpenAPI specification generated successfully: openapi-cli.json" -ForegroundColor Green

        Write-Host "Creating backup of existing file..." -ForegroundColor Yellow    } else {

        Copy-Item $outputFile $backupFile        Write-Host "CLI method failed. Trying runtime method..." -ForegroundColor Yellow

        Write-Host "✓ Backup created: $backupFile" -ForegroundColor Green        $Method = "runtime"

    }    }

}

    # Generate OpenAPI specification using CLI

    Write-Host "Generating OpenAPI specification using CLI..." -ForegroundColor Yellowif ($Method -eq "runtime") {

    swagger tofile --output $outputFile bin/Release/net8.0-windows/HavenCNCServer.dll v1    # Runtime Method - Start application and download from API

    Write-Host "Using runtime method..." -ForegroundColor Yellow

    if ($LASTEXITCODE -eq 0) {    

        Write-Host "OpenAPI specification generated successfully: $outputFile" -ForegroundColor Green    # Build the project first

    } else {    Write-Host "Building project..." -ForegroundColor Yellow

        Write-Host "CLI method failed. Trying runtime method..." -ForegroundColor Yellow    dotnet build --configuration Release --framework net8.0-windows

        $Method = "runtime"

    }    if ($LASTEXITCODE -ne 0) {

}        Write-Host "Build failed. Cannot generate OpenAPI spec." -ForegroundColor Red

        exit 1

if ($Method -eq "runtime") {    }

    # Runtime Method - Start application and download from API

    Write-Host "Using runtime method..." -ForegroundColor Yellow    Write-Host "Starting HavenCNCServer..." -ForegroundColor Yellow

        

    # Build the project first    # Start the application in the background

    Write-Host "Building project..." -ForegroundColor Yellow    $process = Start-Process -FilePath ".\bin\Release\net8.0-windows\HavenCNCServer.exe" -PassThru

    dotnet build --configuration Release --framework net8.0-windows    

    try {

    if ($LASTEXITCODE -ne 0) {        # Wait for the server to start

        Write-Host "Build failed. Cannot generate OpenAPI spec." -ForegroundColor Red        Write-Host "Waiting for server to start..." -ForegroundColor Yellow

        exit 1        Start-Sleep -Seconds 5

    }        

        # Download the OpenAPI specification

    Write-Host "Starting HavenCNCServer..." -ForegroundColor Yellow        Write-Host "Downloading OpenAPI specification..." -ForegroundColor Yellow

            $maxRetries = 10

    # Start the application in the background        $retryCount = 0

    $process = Start-Process -FilePath ".\bin\Release\net8.0-windows\HavenCNCServer.exe" -PassThru        $success = $false

            

    try {        while ($retryCount -lt $maxRetries -and -not $success) {

        # Wait for the server to start            try {

        Write-Host "Waiting for server to start..." -ForegroundColor Yellow                Invoke-RestMethod -Uri "http://localhost:5000/swagger/v1/swagger.json" -OutFile "openapi-runtime.json" -ErrorAction Stop

        Start-Sleep -Seconds 5                $success = $true

                        Write-Host "OpenAPI specification downloaded successfully: openapi-runtime.json" -ForegroundColor Green

        # Download the OpenAPI specification            }

        Write-Host "Downloading OpenAPI specification..." -ForegroundColor Yellow            catch {

        $maxRetries = 10                $retryCount++

        $retryCount = 0                Write-Host "Retry $retryCount/$maxRetries - Server not ready yet..." -ForegroundColor Yellow

        $success = $false                Start-Sleep -Seconds 2

        $outputFile = "openapi-runtime.json"            }

                }

        # Create backup of existing file if it exists        

        if (Test-Path $outputFile) {        if (-not $success) {

            $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"            Write-Host "Failed to download OpenAPI specification. Server might not be running correctly." -ForegroundColor Red

            $backupFile = "openapi-runtime.backup.$timestamp.json"            Write-Host "You can manually access the API at: http://localhost:5000/swagger" -ForegroundColor Cyan

            Write-Host "Creating backup of existing file..." -ForegroundColor Yellow        }

            Copy-Item $outputFile $backupFile    }

            Write-Host "✓ Backup created: $backupFile" -ForegroundColor Green    finally {

        }        # Clean up - stop the application

                if ($process -and -not $process.HasExited) {

        while ($retryCount -lt $maxRetries -and -not $success) {            Write-Host "Stopping HavenCNCServer..." -ForegroundColor Yellow

            try {            $process.Kill()

                Invoke-RestMethod -Uri "http://localhost:5000/swagger/v1/swagger.json" -OutFile $outputFile -ErrorAction Stop            $process.WaitForExit(5000)

                $success = $true        }

                Write-Host "OpenAPI specification downloaded successfully: $outputFile" -ForegroundColor Green    }

            }}

            catch {

                $retryCount++Write-Host "`nGeneration complete!" -ForegroundColor Green

                Write-Host "Retry $retryCount/$maxRetries - Server not ready yet..." -ForegroundColor YellowWrite-Host "Available endpoints when running:" -ForegroundColor Cyan

                Start-Sleep -Seconds 2Write-Host "  - Swagger UI: http://localhost:5000/swagger" -ForegroundColor Cyan

            }Write-Host "  - OpenAPI JSON: http://localhost:5000/swagger/v1/swagger.json" -ForegroundColor Cyan

        }Write-Host "  - Main App: http://localhost:5000" -ForegroundColor Cyan
        
        if (-not $success) {
            Write-Host "Failed to download OpenAPI specification. Server might not be running correctly." -ForegroundColor Red
            Write-Host "You can manually access the API at: http://localhost:5000/swagger" -ForegroundColor Cyan
        }
    }
    finally {
        # Clean up - stop the application
        if ($process -and -not $process.HasExited) {
            Write-Host "Stopping HavenCNCServer..." -ForegroundColor Yellow
            $process.Kill()
            $process.WaitForExit(5000)
        }
    }
}

Write-Host "`nGeneration complete!" -ForegroundColor Green
Write-Host "Available endpoints when running:" -ForegroundColor Cyan
Write-Host "  - Swagger UI: http://localhost:5000/swagger" -ForegroundColor Cyan
Write-Host "  - OpenAPI JSON: http://localhost:5000/swagger/v1/swagger.json" -ForegroundColor Cyan
Write-Host "  - Main App: http://localhost:5000" -ForegroundColor Cyan