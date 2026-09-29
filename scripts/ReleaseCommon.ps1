# PowerShell 7.4+; shared allowlist for distribution, never copy the workspace.
. (Join-Path $PSScriptRoot 'VerificationSafety.ps1')
function Get-ReleaseFiles([string]$Source, [switch]$UnityPackage) {
    Assert-NoVerificationLinks $Source
    foreach ($directory in @('Editor','Runtime')) {
        Get-ChildItem -LiteralPath (Join-Path $Source $directory) -File -Recurse
        Get-Item -LiteralPath (Join-Path $Source ($directory + '.meta'))
    }
    $names = @('LICENSE','LICENSE.meta','README.md','README.md.meta','CHANGELOG.md','CHANGELOG.md.meta')
    if (-not $UnityPackage) { $names += @('package.json','package.json.meta') }
    foreach ($name in $names) { Get-Item -LiteralPath (Join-Path $Source $name) }
}
function Write-ReleaseChecksums([string]$Directory) {
    $lines = foreach ($file in Get-ChildItem -LiteralPath $Directory -File | Where-Object { $_.Extension -in @('.zip','.unitypackage') } | Sort-Object Name) {
        (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + $file.Name
    }
    [IO.File]::WriteAllLines((Join-Path $Directory 'SHA256SUMS.txt'), [string[]]$lines, [Text.UTF8Encoding]::new($false))
}
