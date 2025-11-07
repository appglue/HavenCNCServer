param([switch]$SkipFrontend = $false)

Write-Host "=== HavenCNC Build Script ===" -ForegroundColor Green

if (-not $SkipFrontend) {
    Write-Host "Building React frontend..." -ForegroundColor Cyan
    
    if (Test-Path "..\HavenCNC") {
        Push-Location "..\HavenCNC"
        try {
            Write-Host "Installing dependencies..." -ForegroundColor Yellow
            yarn install
            if ($LASTEXITCODE -ne 0) {
                throw "yarn install failed with exit code $LASTEXITCODE"
            }
            
            Write-Host "Building frontend..." -ForegroundColor Yellow
            yarn build
            if ($LASTEXITCODE -ne 0) {
                throw "yarn build failed with exit code $LASTEXITCODE"
            }
            
            Write-Host "Deploying to server..." -ForegroundColor Yellow
            yarn deploy:clean
            if ($LASTEXITCODE -ne 0) {
                throw "yarn deploy:clean failed with exit code $LASTEXITCODE"
            }
            
            yarn deploy:copy
            if ($LASTEXITCODE -ne 0) {
                throw "yarn deploy:copy failed with exit code $LASTEXITCODE"
            }
            
            Write-Host "Frontend completed" -ForegroundColor Green
        }
        catch {
            Write-Error "Frontend build failed: $_"
            exit 1
        }
        finally {
            Pop-Location
        }
    } else {
        Write-Warning "Frontend directory not found, skipping"
    }
}

Write-Host "Building .NET application..." -ForegroundColor Cyan
dotnet build --configuration Release --framework net8.0-windows
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed"
    exit 1
}
Write-Host "Build completed" -ForegroundColor Green

Write-Host "Creating installer..." -ForegroundColor Cyan
$InnoPath = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 5\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 5\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $InnoPath) {
    Write-Warning "Inno Setup not found. Installer creation skipped."
    Write-Host "To create installer, install Inno Setup from https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
    Write-Host "Build completed without installer." -ForegroundColor Green
    exit 0
}

Write-Host "Using Inno Setup: $InnoPath" -ForegroundColor Yellow
& $InnoPath setup.iss
if ($LASTEXITCODE -ne 0) {
    Write-Error "Installer creation failed"
    exit 1
}

Write-Host "Installer created successfully!" -ForegroundColor Green

# Copy installer to D drive if it exists
if (Test-Path "D:\") {
    $installerPath = "installer\HavenCNCServer-Setup-1.0.0.exe"
    if (Test-Path $installerPath) {
        try {
            Copy-Item $installerPath "D:\" -Force
            Write-Host "Installer copied to D:\" -ForegroundColor Cyan
        }
        catch {
            Write-Warning "Failed to copy installer to D:\: $_"
        }
    }
}