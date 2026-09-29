param(
    [string]$UnityEditor = 'C:/Program Files/Unity/Hub/Editor/2022.3.22f1/Editor/Unity.exe',
    [switch]$IncludeNonToon
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'VerificationSafety.ps1')
$workspace = Split-Path -Parent $PSScriptRoot
$project = Join-Path $workspace '.verification~/Unity'
$log = Join-Path $workspace '.verification~/editmode.log'
$results = Join-Path $workspace '.verification~/editmode-results.xml'
foreach ($path in @($project, $log, $results, (Join-Path $project 'Packages/manifest.json'))) {
    Assert-VerificationPath $path $workspace
}
if (-not (Test-Path -LiteralPath (Join-Path $project 'Packages/manifest.json'))) {
    throw 'Run Prepare-Verification.ps1 first.'
}
$started = [DateTime]::UtcNow
$assemblies = 'Camellian.DistanceFade.Tests.Editor'
if ($IncludeNonToon) { $assemblies += ';Camellian.NonToonDistanceFade.Tests.Editor' }
# Inspector regression tests need a graphics device even in batch mode.
$arguments = @('-batchmode', '-projectPath', ('"' + $project + '"'),
    '-runTests', '-testPlatform', 'EditMode', '-assemblyNames', $assemblies,
    '-testResults', ('"' + $results + '"'), '-logFile', ('"' + $log + '"'))
$process = Start-Process -FilePath $UnityEditor -ArgumentList $arguments -WindowStyle Hidden -PassThru
# Wait for the Editor itself; Start-Process -Wait also waits for long-lived child services.
$process.WaitForExit()
if ($process.ExitCode -ne 0) { throw "Unity tests failed (exit $($process.ExitCode)). See $log" }
if (-not (Test-Path -LiteralPath $results) -or (Get-Item -LiteralPath $results).LastWriteTimeUtc -lt $started) {
    throw 'Unity did not produce fresh test results.'
}
[xml]$report = Get-Content -LiteralPath $results -Raw
if ($report.'test-run'.result -ne 'Passed' -or [int]$report.'test-run'.total -eq 0) {
    throw "Tests did not pass. See $results"
}
$report.'test-run' | Select-Object result, total, passed, failed, skipped
