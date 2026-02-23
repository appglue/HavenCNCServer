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
Write-Host "Run your external installer project to package the output." -ForegroundColor Yellow