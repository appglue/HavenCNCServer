# Deadlock Diagnostic Script for HavenCNCServer
param(
    [switch]$Install,
    [switch]$Analyze,
    [string]$DumpFile
)

$ProcessName = "VirtualControlPanel"

# Install tools
if ($Install) {
    Write-Host "Installing dotnet diagnostic tools..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-dump 2>&1 | Out-Null
    dotnet tool install --global dotnet-trace 2>&1 | Out-Null
    Write-Host "Tools installed successfully!" -ForegroundColor Green
    exit 0
}

# Analyze existing dump
if ($Analyze -or $DumpFile) {
    if (-not $DumpFile) {
        Write-Host "ERROR: -DumpFile parameter required" -ForegroundColor Red
        exit 1
    }
    
    if (-not (Test-Path $DumpFile)) {
        Write-Host "ERROR: Dump file not found: $DumpFile" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "`n=== Analyzing dump for deadlocks ===" -ForegroundColor Cyan
    Write-Host "`nUseful commands inside analyzer:" -ForegroundColor Yellow
    Write-Host "  clrthreads       - Show all managed threads and their state" -ForegroundColor White
    Write-Host "  syncblk          - Show synchronization blocks (LOCKS!)" -ForegroundColor White
    Write-Host "  parallelstacks   - Visual thread grouping" -ForegroundColor White
    Write-Host "  dumpheap -stat   - Object statistics" -ForegroundColor White
    Write-Host "  clrstack         - Stack trace for current thread" -ForegroundColor White
    Write-Host "  setthread <id>   - Switch to specific thread" -ForegroundColor White
    Write-Host "`nType 'exit' to quit analyzer`n" -ForegroundColor Gray
    
    Write-Host "Opening interactive analyzer..." -ForegroundColor Green
    dotnet-dump analyze $DumpFile
    exit 0
}

# Default: Capture dump
$proc = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue
if (-not $proc) {
    Write-Host "`nERROR: Process '$ProcessName' is not running!" -ForegroundColor Red
    Write-Host "`nUsage:" -ForegroundColor Yellow
    Write-Host "  Install tools:  .\diagnose-deadlock.ps1 -Install" -ForegroundColor White
    Write-Host "  Capture dump:   .\diagnose-deadlock.ps1" -ForegroundColor White
    Write-Host "  Analyze dump:   .\diagnose-deadlock.ps1 -Analyze -DumpFile dump.dmp" -ForegroundColor White
    exit 1
}

Write-Host "`n=== Capturing Memory Dump ===" -ForegroundColor Cyan
Write-Host "Process: $ProcessName (PID: $($proc.Id))" -ForegroundColor Yellow
Write-Host "This may take 10-30 seconds..." -ForegroundColor Yellow
Write-Host "`nPress Ctrl+C to cancel, or any key to continue..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$dumpPath = "deadlock_dump_$timestamp.dmp"

dotnet-dump collect -p $proc.Id -o $dumpPath

if (Test-Path $dumpPath) {
    Write-Host "`nSUCCESS! Dump captured: $dumpPath" -ForegroundColor Green
    Write-Host "`nTo analyze, run:" -ForegroundColor Yellow
    Write-Host "  .\diagnose-deadlock.ps1 -Analyze -DumpFile `"$dumpPath`"" -ForegroundColor White
    
    Write-Host "`nAnalyze now? (y/n): " -ForegroundColor Yellow -NoNewline
    $response = Read-Host
    if ($response -eq 'y') {
        & $PSCommandPath -Analyze -DumpFile $dumpPath
    }
}
else {
    Write-Host "`nERROR: Dump file was not created" -ForegroundColor Red
    exit 1
}
