param(
    [string]$UnityEditor = 'C:/Program Files/Unity/Hub/Editor/2022.3.22f1/Editor/Unity.exe'
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
$arguments = @('-batchmode', '-nographics', '-projectPath', ('"' + $project + '"'),
    '-runTests', '-testPlatform', 'EditMode', '-assemblyNames', 'Camellian.DistanceFade.Tests.Editor',
    '-testResults', ('"' + $results + '"'), '-logFile', ('"' + $log + '"'))
$process = Start-Process -FilePath $UnityEditor -ArgumentList $arguments -WindowStyle Hidden -PassThru -Wait
if ($process.ExitCode -ne 0) { throw "Unity tests failed (exit $($process.ExitCode)). See $log" }
if (-not (Test-Path -LiteralPath $results) -or (Get-Item -LiteralPath $results).LastWriteTimeUtc -lt $started) {
    throw 'Unity did not produce fresh test results.'
}
[xml]$report = Get-Content -LiteralPath $results -Raw
if ($report.'test-run'.result -ne 'Passed' -or [int]$report.'test-run'.total -eq 0) {
    throw "Tests did not pass. See $results"
}
$report.'test-run' | Select-Object result, total, passed, failed, skipped
