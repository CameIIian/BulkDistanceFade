param(
    [Parameter(Mandatory = $true)][string]$ShaderCoreSource,
    [Parameter(Mandatory = $true)][string]$NonToonSource
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'VerificationSafety.ps1')
$workspace = Split-Path -Parent $PSScriptRoot
$project = Join-Path $workspace '.verification~/Unity'
$manifestPath = Join-Path $project 'Packages/manifest.json'
Assert-VerificationPath $manifestPath $workspace
if (-not (Test-Path -LiteralPath $manifestPath)) { throw 'Run Prepare-Verification.ps1 first.' }
$sources = @(
    @{ Path = $ShaderCoreSource; Name = 'jp.lilxyzw.shadercore'; Version = '0.1.11' },
    @{ Path = $NonToonSource; Name = 'jp.lilxyzw.nontoon'; Version = '0.1.3' }
)
foreach ($entry in $sources) {
    $source = (Resolve-Path -LiteralPath $entry.Path).ProviderPath
    Assert-VerificationPath (Join-Path $source 'package.json') $source
    Assert-NoVerificationLinks $source
    $info = Get-Content -LiteralPath (Join-Path $source 'package.json') -Raw | ConvertFrom-Json
    if ($info.name -cne $entry.Name -or $info.version -cne $entry.Version) { throw 'Unsupported dependency manifest.' }
    $destination = Join-Path $project ('Packages/' + $entry.Name)
    Assert-VerificationPath $destination $project
    if (Test-Path -LiteralPath $destination) { throw 'Dependency copy already exists; verify it before changing the test environment.' }
}
foreach ($entry in $sources) {
    $source = (Resolve-Path -LiteralPath $entry.Path).ProviderPath
    $destination = Join-Path $project ('Packages/' + $entry.Name)
    New-Item -ItemType Directory -Path $destination | Out-Null
    Get-ChildItem -LiteralPath $source -Force | Where-Object { $_.Name -ne '.git' } |
        ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $destination -Recurse }
}
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$manifest.dependencies | Add-Member -NotePropertyName 'com.camellian.lazyfade.nontoon' -NotePropertyValue ('file:' + (Join-Path $workspace 'Packages/com.camellian.lazyfade.nontoon').Replace('\', '/')) -Force
$manifest.testables = @($manifest.testables | Where-Object { $_ -ne 'com.camellian.lazyfade.nontoon' }) + 'com.camellian.lazyfade.nontoon'
$manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $manifestPath -Encoding utf8
'NonToon verification dependencies prepared.'
