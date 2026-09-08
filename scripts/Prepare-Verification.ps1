param(
    [Parameter(Mandatory = $true)][string]$ReferenceProject
)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
$verification = Join-Path $workspace '.verification~/Unity'
$packageOutput = Join-Path $verification 'Packages'
New-Item -ItemType Directory -Force -Path $packageOutput, (Join-Path $verification 'Assets'), (Join-Path $verification 'ProjectSettings') | Out-Null
$copied = @{}
$manifest = [ordered]@{}
function Copy-Dependency([string]$name) {
    if ($copied.ContainsKey($name)) { return }
    $copied[$name] = $true
    if ($name.StartsWith('com.unity.modules.')) { $manifest[$name] = '1.0.0'; return }
    $source = Join-Path $ReferenceProject "Packages/$name"
    if (-not (Test-Path -LiteralPath (Join-Path $source 'package.json'))) {
        $candidate = Get-ChildItem -LiteralPath (Join-Path $ReferenceProject 'Library/PackageCache') -Directory |
            Where-Object { $_.Name.StartsWith($name + '@') } | Select-Object -First 1
        if ($null -eq $candidate) { throw "Missing dependency in reference project: $name" }
        $source = $candidate.FullName
    }
    $info = Get-Content -LiteralPath (Join-Path $source 'package.json') -Raw | ConvertFrom-Json
    $destination = Join-Path $packageOutput $name
    if (-not (Test-Path -LiteralPath $destination)) { Copy-Item -LiteralPath $source -Destination $destination -Recurse }
    foreach ($field in @('dependencies', 'vpmDependencies')) {
        if ($null -ne $info.$field) {
            foreach ($dependency in $info.$field.PSObject.Properties) { Copy-Dependency $dependency.Name }
        }
    }
    Write-Output "$name $($info.version)"
}
foreach ($name in @('com.vrchat.avatars','nadena.dev.ndmf','jp.lilxyzw.liltoon','com.unity.test-framework')) { Copy-Dependency $name }
# SDK needs the standard Unity built-in modules as well as its declared UPM dependencies.
$referenceManifest = Get-Content -LiteralPath (Join-Path $ReferenceProject 'Packages/manifest.json') -Raw | ConvertFrom-Json
foreach ($property in $referenceManifest.dependencies.PSObject.Properties) {
    if ($property.Name.StartsWith('com.unity.modules.')) { $manifest[$property.Name] = $property.Value }
}
$manifest['com.camellian.liltoon-distance-fade'] = 'file:' + (Join-Path $workspace 'Packages/com.camellian.liltoon-distance-fade').Replace('\', '/')
@{ dependencies = $manifest; testables = @('com.camellian.liltoon-distance-fade') } | ConvertTo-Json -Depth 8 |
    Set-Content -LiteralPath (Join-Path $packageOutput 'manifest.json') -Encoding utf8
Copy-Item -LiteralPath (Join-Path $ReferenceProject 'ProjectSettings/ProjectVersion.txt') -Destination (Join-Path $verification 'ProjectSettings/ProjectVersion.txt')
Write-Output "Verification project: $verification"
