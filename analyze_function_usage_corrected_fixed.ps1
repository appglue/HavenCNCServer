# Search for actual function names from functions.xml in PLC logic
# Excludes comments, declarations, and variables - only searches for function usage

$functionNamesFile = "c:\HavenCNCServer\actual_function_names_from_xml.txt"
$plcFile = "c:\HavenCNCServer\Centriod\Scripts\acorn_router_plc.src"
$outputFile = "c:\HavenCNCServer\corrected_function_analysis.txt"

Write-Host "Starting corrected function analysis..."

# Read the actual function names
$functionNames = Get-Content $functionNamesFile | Where-Object { $_ -ne "" }
Write-Host "Loaded $($functionNames.Count) function names from functions.xml"

# Read PLC file content starting from line 3000
$plcContent = Get-Content $plcFile
$plcLogicLines = $plcContent[2999..($plcContent.Count - 1)]  # Start from line 3000 (0-indexed)
Write-Host "Scanning $($plcLogicLines.Count) lines of PLC logic starting from line 3000"

$foundFunctions = @{}
$totalMatches = 0

foreach ($functionName in $functionNames) {
    $matchList = New-Object System.Collections.ArrayList
    $lineNumber = 3000  # Starting line number
    
    foreach ($line in $plcLogicLines) {
        $originalLine = $line
        $line = $line.Trim()
        
        # Skip empty lines
        if ([string]::IsNullOrWhiteSpace($line)) {
            $lineNumber++
            continue
        }
        
        # Skip comments (lines starting with ;)
        if ($line.StartsWith(";")) {
            $lineNumber++
            continue
        }
        
        # Skip declarations (lines containing " IS ")
        if ($line -like "* IS *") {
            $lineNumber++
            continue
        }
        
        # Skip variable assignments that look like memory locations
        # Skip lines that contain patterns like m9495_111, SV_M94_M95_XXX, etc.
        if ($line -match "m\d+_\d+" -or $line -match "SV_M\d+_M\d+_" -or $line -match "M94M95\d+") {
            $lineNumber++
            continue
        }
        
        # Look for the function name in the line
        # Use word boundaries to ensure we match complete function names
        if ($line -match "\b$([regex]::Escape($functionName))\b") {
            # Additional validation to ensure this looks like a function usage, not a variable
            # Skip if it appears to be a variable assignment or memory location
            if ($line -notmatch "^\s*$([regex]::Escape($functionName))\s*=" -and 
                $line -notmatch "_$([regex]::Escape($functionName))_" -and
                $line -notmatch "$([regex]::Escape($functionName))_\d+") {
                
                $matchObj = New-Object PSObject -Property @{
                    LineNumber = $lineNumber
                    Line = $originalLine.Trim()
                }
                $matchList.Add($matchObj) | Out-Null
                $totalMatches++
            }
        }
        $lineNumber++
    }
    
    if ($matchList.Count -gt 0) {
        $foundFunctions[$functionName] = $matchList
    }
}

# Generate report
$report = @()
$report += "=============================================================================="
$report += "CORRECTED FUNCTION ANALYSIS REPORT"
$report += "Generated: $(Get-Date)"
$report += "=============================================================================="
$report += ""
$report += "SUMMARY:"
$report += "- Total function names from functions.xml: $($functionNames.Count)"
$report += "- Functions found in PLC logic (after line 3000): $($foundFunctions.Count)"
$report += "- Total usage instances found: $totalMatches"
$report += "- Percentage of functions used: $([math]::Round(($foundFunctions.Count / $functionNames.Count) * 100, 2))%"
$report += ""
$report += "=============================================================================="
$report += "FUNCTIONS FOUND IN PLC LOGIC:"
$report += "=============================================================================="
$report += ""

$sortedFunctions = $foundFunctions.GetEnumerator() | Sort-Object Name
foreach ($func in $sortedFunctions) {
    $report += "Function: $($func.Name)"
    $report += "  Usage count: $($func.Value.Count)"
    $report += "  Found at lines:"
    foreach ($match in $func.Value) {
        $report += "    Line $($match.LineNumber): $($match.Line)"
    }
    $report += ""
}

$report += "=============================================================================="
$report += "FUNCTIONS NOT FOUND IN PLC LOGIC:"
$report += "=============================================================================="
$report += ""

$notFoundFunctions = $functionNames | Where-Object { $_ -notin $foundFunctions.Keys } | Sort-Object
foreach ($func in $notFoundFunctions) {
    $report += $func
}

# Write report to file
$report | Out-File -FilePath $outputFile -Encoding UTF8

Write-Host ""
Write-Host "Analysis complete!"
Write-Host "Report written to: $outputFile"
Write-Host ""
Write-Host "Summary:"
Write-Host "- Functions found: $($foundFunctions.Count)"
Write-Host "- Functions not found: $($notFoundFunctions.Count)"
Write-Host "- Total usage instances: $totalMatches"