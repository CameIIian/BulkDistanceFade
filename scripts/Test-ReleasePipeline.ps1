#Requires -Version 7.4
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ReleaseCommon.ps1')
$workspace = Split-Path -Parent $PSScriptRoot
$fixture = Join-Path $workspace ('.verification~/publication-tests/' + [Guid]::NewGuid().ToString('N'))
Assert-VerificationPath $fixture $workspace
New-Item -ItemType Directory -Path $fixture | Out-Null
foreach ($name in @('Packages','scripts','docs','design','release','README.md','LICENSE','.gitignore','.gitattributes')) {
    Copy-Item -LiteralPath (Join-Path $workspace $name) -Destination $fixture -Recurse
}
# Keep the existing two-edition regression fixture on one version.
$nonToonManifest = Join-Path $fixture 'Packages/com.camellian.lazyfade.nontoon/package.json'
$fixtureInfo = Get-Content -LiteralPath $nonToonManifest -Raw | ConvertFrom-Json
$fixtureInfo.version = (Get-Content -LiteralPath (Join-Path $fixture 'Packages/com.camellian.lazyfade.liltoon/package.json') -Raw | ConvertFrom-Json).version
$fixtureInfo | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath $nonToonManifest
# Tests are offline and isolated; never put sample identity in the real listing.
$listingPath = Join-Path $fixture 'docs/index.json'
if (Test-Path -LiteralPath $listingPath) {
    Assert-VerificationPath $listingPath $fixture
    Move-Item -LiteralPath $listingPath -Destination (Join-Path $fixture 'original-listing.json')
}
$builder = Join-Path $fixture 'scripts/Build-VpmRepository.ps1'
$scanner = Join-Path $fixture 'scripts/Test-PublicationSafety.ps1'
function Assert-True([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
function Assert-Fails([scriptblock]$Action, [string]$Expected) {
    $caught = $false
    try { & $Action | Out-Null } catch { $caught = $true; Assert-True ($_.Exception.Message -like "*$Expected*") ('Unexpected error: ' + $_.Exception.Message) }
    Assert-True $caught ('Expected failure: ' + $Expected)
}
& $builder -Repository 'lazyfade-test/lazyFade' -PublicEmail 'release@example.com' | Out-Null
$listing = Get-Content -LiteralPath $listingPath -Raw | ConvertFrom-Json -AsHashtable
Assert-True ($listing.packages.Count -eq 2) 'Expected both packages.'
$version = (Get-Content -LiteralPath (Join-Path $fixture 'Packages/com.camellian.lazyfade.liltoon/package.json') -Raw | ConvertFrom-Json).version
$output = Join-Path $fixture "artifacts/$version"
function Assert-ArchivesClosed([string]$Phase) {
    foreach ($file in Get-ChildItem -LiteralPath $output -Filter '*.zip') {
        # Antivirus/indexing can briefly open a newly written archive on Windows.
        for ($attempt = 0; $attempt -lt 20; $attempt++) {
            try { $handle = [IO.File]::Open($file.FullName, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None); $handle.Dispose(); break }
            catch [IO.IOException] {
                if ($attempt -eq 19) { throw "Archive remained locked: $Phase" }
                Start-Sleep -Milliseconds 250
            }
        }
    }
}
Assert-ArchivesClosed 'first build'
$before = @{}
foreach ($zip in Get-ChildItem -LiteralPath $output -Filter '*.zip') {
    $hash = (Get-FileHash -LiteralPath $zip.FullName).Hash.ToLowerInvariant()
    $before[$zip.Name] = $hash
    $edition = if ($zip.Name.Contains('NonToon')) { 'nontoon' } else { 'liltoon' }
    $entry = $listing.packages["com.camellian.lazyfade.$edition"].versions[$version]
    Assert-True ($entry.zipSHA256 -ceq $hash) 'Listing hash mismatch.'
    Assert-True ($entry.url -ceq "https://github.com/lazyfade-test/lazyFade/releases/download/v$version/$($zip.Name)") 'Release URL mismatch.'
    $archiveMemory = [IO.MemoryStream]::new([IO.File]::ReadAllBytes($zip.FullName), $false)
    $archive = [IO.Compression.ZipArchive]::new($archiveMemory, [IO.Compression.ZipArchiveMode]::Read, $false)
    try {
        $manifestEntry = $archive.GetEntry('package.json')
        Assert-True ($null -ne $manifestEntry) 'Manifest must be at ZIP root.'
        $reader = [IO.StreamReader]::new($manifestEntry.Open())
        try { $manifest = $reader.ReadToEnd() | ConvertFrom-Json -AsHashtable } finally { $reader.Dispose() }
        Assert-True (-not $manifest.ContainsKey('zipSHA256')) 'ZIP must not contain its own checksum.'
        Assert-True ($manifest.author.email -eq 'release@example.com') 'Missing public email.'
        Assert-True ($manifest.vpmDependencies.ContainsKey('nadena.dev.ndmf')) 'Missing dependencies.'
    } finally { $archive.Dispose(); $archiveMemory.Dispose() }
}
Assert-True ($before.Count -eq 2) 'Expected two ZIP files.'
Assert-ArchivesClosed 'manifest inspection'
$id = 'com.camellian.lazyfade.liltoon'
$listing.packages[$id].versions['0.0.1'] = @{ name = $id; version = '0.0.1'; url = 'https://example.com/old.zip'; zipSHA256 = '0' * 64 }
[IO.File]::WriteAllText($listingPath, ($listing | ConvertTo-Json -Depth 40))
& $builder -Repository 'lazyfade-test/lazyFade' -PublicEmail 'release@example.com' | Out-Null
Assert-ArchivesClosed 'second build'
foreach ($zip in Get-ChildItem -LiteralPath $output -Filter '*.zip') {
    Assert-True ((Get-FileHash -LiteralPath $zip.FullName).Hash.ToLowerInvariant() -ceq $before[$zip.Name]) 'Non-deterministic ZIP.'
}
$listing = Get-Content -LiteralPath $listingPath -Raw | ConvertFrom-Json -AsHashtable
Assert-True ($listing.packages[$id].versions.ContainsKey('0.0.1')) 'Lost previous version.'
$oldNonToon = $listing.packages['com.camellian.lazyfade.nontoon'].versions[$version] | ConvertTo-Json -Depth 30 -Compress
$oldLilToon = $listing.packages[$id] | ConvertTo-Json -Depth 30 -Compress
$fixtureInfo.version = '0.2.1'
$fixtureInfo | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath $nonToonManifest
& $builder -Repository 'lazyfade-test/lazyFade' -PublicEmail 'release@example.com' -Edition NonToon | Out-Null
$listing = Get-Content -LiteralPath $listingPath -Raw | ConvertFrom-Json -AsHashtable
Assert-True (($listing.packages[$id] | ConvertTo-Json -Depth 30 -Compress) -ceq $oldLilToon) 'Single-edition release changed lilToon.'
Assert-True (($listing.packages['com.camellian.lazyfade.nontoon'].versions[$version] | ConvertTo-Json -Depth 30 -Compress) -ceq $oldNonToon) 'Single-edition release changed previous NonToon.'
Assert-True ($listing.packages['com.camellian.lazyfade.nontoon'].versions.ContainsKey('0.2.1')) 'Missing new NonToon release.'
Assert-True (-not (Test-Path (Join-Path $fixture 'artifacts/0.2.1/lazyFade-lilToon-0.2.1.zip'))) 'Unexpected lilToon release.'
Assert-Fails { & $builder -Repository 'different-owner/lazyFade' -PublicEmail 'release@example.com' } 'identity differs'
Assert-Fails { & $builder -Repository 'lazyfade-test/lazyFade' -PublicEmail 'release@example.com' -ListingUrl 'http://example.com/index.json' } 'public HTTPS'
$source = Join-Path $fixture 'Packages/com.camellian.lazyfade.liltoon/Runtime/LazyFadeLilToon.cs'
$original = [IO.File]::ReadAllText($source)
[IO.File]::WriteAllText($source, $original + "`n// Changed after release.`n")
Assert-Fails { & $builder -Repository 'lazyfade-test/lazyFade' -PublicEmail 'release@example.com' } 'different content'
Assert-Fails { & $scanner } 'ZIP content differs'
Assert-ArchivesClosed 'rejected archive scan'
[IO.File]::WriteAllText($source, $original)
# Fake credentials are synthesized inside an ignored fixture and never logged.
[IO.File]::WriteAllText($source, $original + "`n// " + ('gh' + 'p_' + ('a' * 36)))
Assert-Fails { & $scanner -SourceOnly } 'Publication check failed'
[IO.File]::WriteAllText($source, $original)
$archivePath = (Get-ChildItem -LiteralPath $output -Filter '*.zip' | Select-Object -First 1).FullName
Assert-ArchivesClosed 'before malicious archive test'
$archive = [IO.Compression.ZipFile]::Open($archivePath, [IO.Compression.ZipArchiveMode]::Update)
try { $null = $archive.CreateEntry('../outside.txt') } finally { $archive.Dispose() }
Assert-Fails { & $scanner } 'Unexpected or duplicate ZIP entry'
'Release pipeline passed: ZIP layout, manifest/dependencies, hashes, repeatability, history retention, immutable releases, URL validation, secret detection and archive traversal rejection.'
