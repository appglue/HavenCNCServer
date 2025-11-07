# Script to update functions.xml files - hide functions not in approved list

# Define approved function names from Function_Categories_by_Type.md
$approvedFunctions = @(
    'AirBlowNozzle', 'AirPressureLowMessage', 'AirPressureLowStop', 'AlignmentLaserEnable_O',
    'ATC_AirPressureOk', 'ATCAirBlowActivate', 'Axis1DriveOk', 'Axis2DriveOk', 'Axis3DriveOk', 'Axis4DriveOk',
    'CycleCancel2', 'CycleStart2', 'DrawBarIsDown_I', 'DrawBarIsUp_I', 'DrawBarReleased', 'DrawBarUp_O',
    'DriveOk', 'DriveResetOut', 'DustCollectionOn', 'DustFootActivate', 'EStopOk', 'EStopOk2',
    'FeedHold2', 'FiberLaserOk_I', 'FiberLaserOn_I', 'FiberLaserReset_O', 'FirstAxisHomeLimitOk',
    'FirstAxisHomeOk', 'Flood', 'HomeAll', 'HomeLimitAll', 'LaserAlignActivate', 'LaserCooling_Fan',
    'LaserDeploy_O', 'LaserEnable', 'LaserEnable_O', 'LaserHeadInPos_I', 'LaserHeadOk_I', 'LaserReady_I',
    'LaserReset', 'LaserStandby_O', 'LubeOk', 'LubePump', 'Mist', 'NoFaultOut', 'OUTPUT1', 'OUTPUT2',
    'OUTPUT3', 'OUTPUT4', 'OUTPUT5', 'OUTPUT6', 'OUTPUT7', 'OUTPUT8', 'PopUpPins', 'ProbeDetect',
    'ProbeTripped', 'PWMSelect', 'RouterDustCollection', 'RouterVacuumHoldDown', 'SafetyDoorLockOpen_O',
    'SecondAxisHomeLimitOk', 'SecondAxisHomeOk', 'SlavedHomeInput', 'SpindleBrakeRelease', 'SpindleCooling',
    'SpindleCooling_Fan', 'SpindleOk', 'SpinFWD', 'SpinREV', 'ToolCheck2', 'ToolClamped_I', 'ToolIsPresent_I',
    'ToolIsUnclamped', 'ToolUnclampButton', 'TorchArcOk_I', 'TorchFloatSwitch_I', 'UnclampTool',
    'VFDDirection_O', 'VFDEnable_O', 'VFDResetOut_O', 'WorkLight'
)

function Update-FunctionsXml {
    param(
        [string]$FilePath,
        [string[]]$ApprovedFunctions
    )
    
    Write-Host "Updating $FilePath..."
    
    # Load XML content
    [xml]$xml = Get-Content $FilePath
    
    $hiddenCount = 0
    $visibleCount = 0
    
    # Process each PlcFunction element
    foreach ($function in $xml.Functions.PlcFunction) {
        $functionName = $function.Name
        
        if ($functionName -in $ApprovedFunctions) {
            # Function is approved - set Hidden to false
            $function.Hidden = "false"
            $visibleCount++
            Write-Host "  Keeping visible: $functionName"
        } else {
            # Function is not approved - set Hidden to true
            $function.Hidden = "true"
            $hiddenCount++
            Write-Host "  Hiding: $functionName"
        }
    }
    
    # Save the updated XML
    $xml.Save($FilePath)
    
    Write-Host "Updated $FilePath - Hidden: $hiddenCount, Visible: $visibleCount"
    
    return @{
        Hidden = $hiddenCount
        Visible = $visibleCount
    }
}

# File paths
$files = @(
    "c:\HavenCNCServer\Centriod\Scripts\functions.xml",
    "c:\HavenCNCServer\wwwroot\functions.xml"
)

$totalHidden = 0
$totalVisible = 0

# Update both files
foreach ($file in $files) {
    $result = Update-FunctionsXml -FilePath $file -ApprovedFunctions $approvedFunctions
    $totalHidden += $result.Hidden
    $totalVisible += $result.Visible
}

Write-Host ""
Write-Host "=== SUMMARY ==="
Write-Host "Total functions processed across both files:"
Write-Host "  Hidden: $totalHidden"
Write-Host "  Visible: $totalVisible"
Write-Host "  Approved functions in list: $($approvedFunctions.Count)"
Write-Host ""
Write-Host "Functions updated successfully!"