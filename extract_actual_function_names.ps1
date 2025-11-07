# Extract actual function names from functions.xml
# Only extracts <Name> tags within <PlcFunction> elements

$xmlFile = "c:\HavenCNCServer\Centriod\Scripts\functions.xml"
$outputFile = "c:\HavenCNCServer\actual_function_names_from_xml.txt"

Write-Host "Extracting actual function names from functions.xml..."

try {
    # Load the XML file
    [xml]$xml = Get-Content $xmlFile
    
    # Extract all Name elements within PlcFunction elements
    $functionNames = $xml.Functions.PlcFunction | ForEach-Object { $_.Name } | Where-Object { $_ -ne $null -and $_ -ne "" }
    
    # Sort the names alphabetically and remove duplicates
    $uniqueFunctionNames = $functionNames | Sort-Object | Get-Unique
    
    # Write to output file
    $uniqueFunctionNames | Out-File -FilePath $outputFile -Encoding UTF8
    
    Write-Host "Found $($uniqueFunctionNames.Count) unique function names"
    Write-Host "Function names written to: $outputFile"
    
    # Display first 10 function names as preview
    Write-Host "`nFirst 10 function names:"
    $uniqueFunctionNames | Select-Object -First 10 | ForEach-Object { Write-Host "  $_" }
    
    if ($uniqueFunctionNames.Count -gt 10) {
        Write-Host "  ... and $($uniqueFunctionNames.Count - 10) more"
    }
    
} catch {
    Write-Error "Error processing XML file: $($_.Exception.Message)"
}