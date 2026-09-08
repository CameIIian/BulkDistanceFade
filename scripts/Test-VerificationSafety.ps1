$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'VerificationSafety.ps1')
$workspace = Split-Path -Parent $PSScriptRoot
$scratch = Join-Path $workspace ('.verification~/safety-tests/' + [Guid]::NewGuid().ToString('N'))
Assert-VerificationPath $scratch $workspace
New-Item -ItemType Directory -Path $scratch -Force | Out-Null
$passed = 0

function Expect-Rejection([scriptblock]$Action) {
    $rejected = $false
    try { & $Action | Out-Null } catch { $rejected = $true }
    if (-not $rejected) { throw 'Unsafe input was accepted.' }
}

foreach ($name in @('../outside', '..\outside', 'C:\outside', '/outside', 'com.test/../../outside', 'com..test', 'com.test.')) {
    Expect-Rejection { Assert-PackageName $name }
    $passed++
}
foreach ($name in @('com.vrchat.avatars', 'nadena.dev.ndmf', 'jp.lilxyzw.liltoon', 'com.unity.test-framework')) {
    Assert-PackageName $name
    $passed++
}
Expect-Rejection { Assert-VerificationPath (Join-Path $scratch '../outside') $scratch }
$passed++
Expect-Rejection { Assert-VerificationPath ($scratch + '-sibling/file') $scratch }
$passed++
Assert-VerificationPath (Join-Path $scratch 'new/file') $scratch
$passed++

# Exercise the real preparation script against synthetic projects, including
# its copy and manifest output. All fixtures stay in the ignored scratch tree.
function New-Fixture([string]$Name, [hashtable]$Dependencies) {
    $base = Join-Path $scratch $Name
    $reference = Join-Path $base 'reference'
    $runner = Join-Path $base 'workspace/scripts'
    New-Item -ItemType Directory -Path $runner, (Join-Path $reference 'ProjectSettings') -Force | Out-Null
    foreach ($file in @('Prepare-Verification.ps1', 'VerificationSafety.ps1')) {
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot $file) -Destination $runner
    }
    foreach ($name in @('com.vrchat.avatars', 'nadena.dev.ndmf', 'jp.lilxyzw.liltoon', 'com.unity.test-framework')) {
        $directory = Join-Path $reference "Packages/$name"
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
        $deps = @{}
        if ($name -eq 'com.vrchat.avatars') { $deps = $Dependencies }
        @{ name = $name; version = '1.0.0'; dependencies = $deps } | ConvertTo-Json -Depth 8 |
            Set-Content -LiteralPath (Join-Path $directory 'package.json') -Encoding utf8
    }
    @{ dependencies = @{ 'com.unity.modules.physics' = 'file:untrusted-module' } } | ConvertTo-Json |
        Set-Content -LiteralPath (Join-Path $reference 'Packages/manifest.json') -Encoding utf8
    'm_EditorVersion: 2022.3.22f1' | Set-Content -LiteralPath (Join-Path $reference 'ProjectSettings/ProjectVersion.txt')
    return @{ Reference = $reference; Runner = $runner; Base = $base }
}

$valid = New-Fixture 'valid' @{ 'com.unity.modules.physics' = '1.0.0' }
& (Join-Path $valid.Runner 'Prepare-Verification.ps1') -ReferenceProject $valid.Reference | Out-Null
$output = Join-Path $valid.Base 'workspace/.verification~/Unity'
$manifest = Get-Content -LiteralPath (Join-Path $output 'Packages/manifest.json') -Raw | ConvertFrom-Json
if ($manifest.dependencies.'com.unity.modules.physics' -ne '1.0.0' -or
    -not (Test-Path -LiteralPath (Join-Path $output 'Packages/com.vrchat.avatars/package.json'))) {
    throw 'Valid dependency preparation failed.'
}
$passed++
$malicious = New-Fixture 'traversal' @{ '../outside' = '1.0.0' }
Expect-Rejection { & (Join-Path $malicious.Runner 'Prepare-Verification.ps1') -ReferenceProject $malicious.Reference }
$passed++
$mismatch = New-Fixture 'mismatch' @{}
'{"name":"com.unexpected.package","version":"1.0.0"}' |
    Set-Content -LiteralPath (Join-Path $mismatch.Reference 'Packages/com.vrchat.avatars/package.json')
Expect-Rejection { & (Join-Path $mismatch.Runner 'Prepare-Verification.ps1') -ReferenceProject $mismatch.Reference }
$passed++

# Directory junctions on Windows do not require symbolic-link privileges.
$linked = New-Fixture 'junction' @{}
$target = Join-Path $scratch 'junction-target'
New-Item -ItemType Directory -Path $target | Out-Null
$junction = Join-Path $linked.Reference 'Packages/com.vrchat.avatars/linked'
New-Item -ItemType Junction -Path $junction -Target $target | Out-Null
Expect-Rejection { & (Join-Path $linked.Runner 'Prepare-Verification.ps1') -ReferenceProject $linked.Reference }
$passed++
Expect-Rejection { Assert-VerificationPath (Join-Path $junction 'new-file') $scratch }
$passed++
Write-Output "Verification safety checks passed: $passed"
