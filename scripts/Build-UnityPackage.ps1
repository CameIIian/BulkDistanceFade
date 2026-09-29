param(
    [string]$UnityEditor = 'C:/Program Files/Unity/Hub/Editor/2022.3.22f1/Editor/Unity.exe',
    [ValidateSet('All','lilToon','NonToon')][string]$Edition = 'All'
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'VerificationSafety.ps1')
$workspace = Split-Path -Parent $PSScriptRoot
if ($Edition -eq 'All') {
    foreach ($variant in @('lilToon','NonToon')) { & $PSCommandPath -UnityEditor $UnityEditor -Edition $variant }
    return
}
& (Join-Path $PSScriptRoot 'Test-PublicationSafety.ps1') -SourceOnly
$source = Join-Path $workspace ('Packages/com.camellian.lazyfade.' + $Edition.ToLowerInvariant())
$component = if ($Edition -eq 'lilToon') { 'LazyFadeLilToon' } else { 'LazyFadeNonToon' }
$folder = 'lazyFade-' + $Edition
$folderGuid = if ($Edition -eq 'lilToon') { '0eb87768450e4c00a924f5e1db5e9e17' } else { 'cc32e35eeec044cb8a13f05332944abe' }
$reference = Join-Path $workspace '.verification~/Unity'
$info = Get-Content -LiteralPath (Join-Path $source 'package.json') -Raw | ConvertFrom-Json
if ($info.version -notmatch '\A[0-9]+\.[0-9]+\.[0-9]+(?:-[a-zA-Z0-9.-]+)?\z') { throw 'Invalid release version.' }
$filename = $folder + '-' + $info.version + '.unitypackage'
$release = Join-Path $workspace ('artifacts/' + $info.version)
$output = Join-Path $release $filename
$notes = Join-Path $workspace ('release/' + $info.version + '.md')
if (-not (Test-Path -LiteralPath $notes)) { throw 'Release notes are missing for this version.' }
Assert-VerificationPath $output $workspace
if (Test-Path -LiteralPath $output) { throw 'Release artifact already exists. Preserve it or move it before rebuilding.' }
Assert-VerificationPath (Join-Path $reference 'Packages/manifest.json') $workspace
if (-not (Test-Path -LiteralPath (Join-Path $reference 'Packages/manifest.json'))) { throw 'Run Prepare-Verification.ps1 first.' }
Assert-NoVerificationLinks $source

# Isolate the Assets distribution from the installed Packages version and test project.
$job = Join-Path $workspace ('.verification~/release-' + [Guid]::NewGuid().ToString('N'))
$stage = Join-Path $job ('Assets/' + $folder)
Assert-VerificationPath $stage $workspace
New-Item -ItemType Directory -Path $stage, (Join-Path $job 'Assets/Editor'), (Join-Path $job 'Packages'), (Join-Path $job 'ProjectSettings') | Out-Null
$manifest = Get-Content -LiteralPath (Join-Path $reference 'Packages/manifest.json') -Raw | ConvertFrom-Json
$dependencies = [ordered]@{}
foreach ($dependency in $manifest.dependencies.PSObject.Properties) {
    if ($dependency.Name.StartsWith('com.unity.modules.')) {
        Assert-PackageName $dependency.Name
        $dependencies[$dependency.Name] = '1.0.0'
    }
}
foreach ($directory in Get-ChildItem -LiteralPath (Join-Path $reference 'Packages') -Directory) {
    Assert-PackageName $directory.Name
    Assert-VerificationPath (Join-Path $directory.FullName 'package.json') $reference
    $dependencyInfo = Get-Content -LiteralPath (Join-Path $directory.FullName 'package.json') -Raw | ConvertFrom-Json
    if ($dependencyInfo.name -cne $directory.Name) { throw 'Dependency name mismatch.' }
    if ($dependencyInfo.name -eq $info.name) { continue }
    $dependencies[$directory.Name] = 'file:' + $directory.FullName.Replace('\', '/')
}
@{ dependencies = $dependencies } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $job 'Packages/manifest.json') -Encoding utf8
Assert-VerificationPath (Join-Path $reference 'ProjectSettings/ProjectVersion.txt') $reference
Copy-Item -LiteralPath (Join-Path $reference 'ProjectSettings/ProjectVersion.txt') -Destination (Join-Path $job 'ProjectSettings/ProjectVersion.txt')

# Allowlist only the product code and distribution documents. Do not include dependencies.
foreach ($name in @('Editor', 'Runtime', 'Editor.meta', 'Runtime.meta', 'LICENSE', 'LICENSE.meta', 'CHANGELOG.md', 'CHANGELOG.md.meta', 'README.md.meta')) {
    Copy-Item -LiteralPath (Join-Path $source $name) -Destination $stage -Recurse
}
Copy-Item -LiteralPath (Join-Path $source 'README.md') -Destination (Join-Path $stage 'README.md')
(@'
fileFormatVersion: 2
guid: __GUID__
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData:
  assetBundleName:
  assetBundleVariant:
'@).Replace('__GUID__', $folderGuid) | Set-Content -LiteralPath ($stage + '.meta') -Encoding utf8

# This helper stays outside the exported folder.
(@'
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class DistanceFadeReleaseExport
{
    public static void Run()
    {
        try
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            const string root = "Assets/__FOLDER__";
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(root + "/Runtime/__COMPONENT__.cs");
            if (script == null || script.GetClass() == null) throw new Exception("Component did not compile.");
            var output = Path.Combine(projectRoot, "lazyFade.unitypackage");
            AssetDatabase.ExportPackage(root, output, ExportPackageOptions.Recurse);
            if (!File.Exists(output) || new FileInfo(output).Length == 0) throw new Exception("Empty export.");
            Debug.Log("lazyFade export passed.");
        }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
    }

    public static void Verify()
    {
        var script = AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/__FOLDER__/Runtime/__COMPONENT__.cs");
        if (script == null || script.GetClass() == null) throw new Exception("Imported component did not compile.");
        File.WriteAllText(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "verified.txt"),
            "Imported component compiled successfully.");
    }
}
'@).Replace('__FOLDER__', $folder).Replace('__COMPONENT__', $component) | Set-Content -LiteralPath (Join-Path $job 'Assets/Editor/DistanceFadeReleaseExport.cs') -Encoding utf8

function Invoke-ReleaseUnity([string]$Phase, [string[]]$ExtraArguments) {
    $log = Join-Path $job ($Phase + '.log')
    $arguments = @('-batchmode', '-nographics', '-quit', '-projectPath', ('"' + $job + '"'),
        '-logFile', ('"' + $log + '"')) + $ExtraArguments
    $process = Start-Process -FilePath $UnityEditor -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) { throw "Unity $Phase failed. See $log" }
}
Invoke-ReleaseUnity 'export' @('-executeMethod', 'DistanceFadeReleaseExport.Run')
$exported = Join-Path $job 'lazyFade.unitypackage'
if (-not (Test-Path -LiteralPath $exported) -or (Get-Item -LiteralPath $exported).Length -eq 0) { throw 'Missing export.' }

# ImportPackage is asynchronous; use separate Unity sessions for import and verification.
# Preserve the exact pre-export files outside Assets for a byte-for-byte comparison.
$expectedDirectory = Join-Path $job 'Expected'
foreach ($path in @($stage, ($stage + '.meta'), $expectedDirectory)) { Assert-VerificationPath $path $job }
New-Item -ItemType Directory -Path $expectedDirectory | Out-Null
Move-Item -LiteralPath $stage -Destination $expectedDirectory
Move-Item -LiteralPath ($stage + '.meta') -Destination $expectedDirectory
Invoke-ReleaseUnity 'import' @('-importPackage', ('"' + $exported + '"'))
Invoke-ReleaseUnity 'verify' @('-executeMethod', 'DistanceFadeReleaseExport.Verify')
if (-not (Test-Path -LiteralPath (Join-Path $job 'verified.txt'))) { throw 'Missing import verification marker.' }
$expectedFiles = @(Get-ChildItem -LiteralPath $expectedDirectory -Recurse -File)
$actualFiles = @(Get-ChildItem -LiteralPath $stage -Recurse -File) + @(Get-Item -LiteralPath ($stage + '.meta'))
if ($actualFiles.Count -ne $expectedFiles.Count) { throw 'Imported file count mismatch.' }
foreach ($file in $expectedFiles) {
    $relative = $file.FullName.Substring($expectedDirectory.Length + 1)
    $imported = Join-Path (Join-Path $job 'Assets') $relative
    if (-not (Test-Path -LiteralPath $imported) -or
        (Get-FileHash -LiteralPath $file.FullName).Hash -ne (Get-FileHash -LiteralPath $imported).Hash) {
        throw "Imported content mismatch: $relative"
    }
}
New-Item -ItemType Directory -Force -Path $release | Out-Null
Copy-Item -LiteralPath $exported -Destination $output
$hash = (Get-FileHash -LiteralPath $output -Algorithm SHA256).Hash.ToLowerInvariant()
$checksums = Join-Path $release 'SHA256SUMS.txt'
Assert-VerificationPath $checksums $workspace
($hash + '  ' + $filename) | Add-Content -LiteralPath $checksums -Encoding ascii
Copy-Item -LiteralPath $notes -Destination (Join-Path $release 'RELEASE_NOTES.md')
Get-Content -LiteralPath (Join-Path $job 'verified.txt')
Write-Output "Export/import round-trip passed: $($expectedFiles.Count) files (including meta)."
Get-Item -LiteralPath $output | Select-Object Name, Length
Write-Output "SHA256: $hash"
