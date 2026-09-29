#Requires -Version 7.4
param(
    [Parameter(Mandatory)][ValidatePattern('^[A-Za-z0-9][A-Za-z0-9-]*/[A-Za-z0-9][A-Za-z0-9_.-]*$')][string]$Repository,
    [Parameter(Mandatory)][string]$PublicEmail,
    [string]$ListingUrl
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ReleaseCommon.ps1')
$workspace = Split-Path -Parent $PSScriptRoot
$email = [Net.Mail.MailAddress]::new($PublicEmail)
if ($email.Address -cne $PublicEmail -or $PublicEmail -match '[\r\n]') { throw 'Use a plain public contact email address.' }
$owner, $repo = $Repository.Split('/')
if (-not $ListingUrl) {
    $base = if ($repo -ieq "$owner.github.io") { "https://$owner.github.io" } else { "https://$owner.github.io/$repo" }
    $ListingUrl = "$base/index.json"
}
$uri = [Uri]$ListingUrl
if (-not $uri.IsAbsoluteUri -or $uri.Scheme -ne 'https' -or $uri.UserInfo -or $uri.Query -or $uri.Fragment -or -not $uri.AbsolutePath.EndsWith('/index.json')) {
    throw 'ListingUrl must be a public HTTPS URL ending in /index.json, without credentials, query or fragment.'
}
$listingPath = Join-Path $workspace 'docs/index.json'
Assert-VerificationPath $listingPath $workspace
& (Join-Path $PSScriptRoot 'Test-PublicationSafety.ps1') -SourceOnly
$listing = [ordered]@{ name = 'lazyFade'; author = 'camellian'; id = "io.github.$($owner.ToLowerInvariant()).$($repo.ToLowerInvariant())"; url = $ListingUrl; packages = @{} }
if (Test-Path -LiteralPath $listingPath) {
    $previous = Get-Content -LiteralPath $listingPath -Raw | ConvertFrom-Json -AsHashtable
    if ($previous.url -cne $ListingUrl -or $previous.id -cne $listing.id) { throw 'Existing listing identity differs; do not silently move a published repository.' }
    $listing.packages = $previous.packages
}
$pending = [Collections.Generic.List[object]]::new()
$versions = @()
foreach ($edition in @('lilToon','NonToon')) {
    $source = Join-Path $workspace ('Packages/com.camellian.lazyfade.' + $edition.ToLowerInvariant())
    $manifest = Get-Content -LiteralPath (Join-Path $source 'package.json') -Raw | ConvertFrom-Json -AsHashtable
    $version = $manifest.version
    if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'Release version must be a stable SemVer.' }
    $versions += $version
    $filename = "lazyFade-$edition-$version.zip"
    $manifest.author.email = $PublicEmail
    $manifest.url = "https://github.com/$Repository/releases/download/v$version/$filename"
    $manifest.documentationUrl = $ListingUrl.Substring(0, $ListingUrl.Length - 'index.json'.Length)
    $manifest.changelogUrl = "https://github.com/$Repository/releases/tag/v$version"
    $manifest.repo = $ListingUrl
    $memory = [IO.MemoryStream]::new()
    $zip = [IO.Compression.ZipArchive]::new($memory, [IO.Compression.ZipArchiveMode]::Create, $true)
    try {
        foreach ($file in Get-ReleaseFiles $source | Sort-Object FullName) {
            $relative = [IO.Path]::GetRelativePath($source, $file.FullName).Replace('\','/')
            $entry = $zip.CreateEntry($relative, [IO.Compression.CompressionLevel]::Optimal)
            $entry.LastWriteTime = [DateTimeOffset]::new(2026,1,1,0,0,0,[TimeSpan]::Zero)
            $stream = $entry.Open()
            try {
                $bytes = if ($relative -eq 'package.json') { [Text.Encoding]::UTF8.GetBytes(($manifest | ConvertTo-Json -Depth 30) + "`n") } else { [IO.File]::ReadAllBytes($file.FullName) }
                $stream.Write($bytes, 0, $bytes.Length)
            } finally { $stream.Dispose() }
        }
    } finally { $zip.Dispose() }
    $bytes = $memory.ToArray(); $memory.Dispose()
    $hash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()
    $output = Join-Path $workspace "artifacts/$version/$filename"
    Assert-VerificationPath $output $workspace
    if ((Test-Path -LiteralPath $output) -and (Get-FileHash -LiteralPath $output -Algorithm SHA256).Hash.ToLowerInvariant() -cne $hash) {
        throw "Existing $filename has different content. Keep published releases immutable; bump the version."
    }
    # zipSHA256 lives only in the listing to avoid a self-referential ZIP hash.
    $manifest.zipSHA256 = $hash
    if (-not $listing.packages.ContainsKey($manifest.name)) { $listing.packages[$manifest.name] = @{ versions = @{} } }
    $entries = $listing.packages[$manifest.name].versions
    if ($entries.ContainsKey($version) -and ($entries[$version].zipSHA256 -cne $hash -or $entries[$version].url -cne $manifest.url)) {
        throw 'An existing listing version differs. Bump the package version instead of overwriting a published version.'
    }
    $entries[$version] = $manifest
    $pending.Add(@{ Path = $output; Bytes = $bytes })
}
if (@($versions | Select-Object -Unique).Count -ne 1) { throw 'Both editions must share a release version.' }
foreach ($item in $pending) {
    $directory = Split-Path -Parent $item.Path
    New-Item -ItemType Directory -Force -Path $directory | Out-Null
    if (-not (Test-Path -LiteralPath $item.Path)) { [IO.File]::WriteAllBytes($item.Path, $item.Bytes) }
}
[IO.File]::WriteAllText($listingPath, ($listing | ConvertTo-Json -Depth 40) + "`n", [Text.UTF8Encoding]::new($false))
$release = Join-Path $workspace ('artifacts/' + $versions[0])
Write-ReleaseChecksums $release
Copy-Item -LiteralPath (Join-Path $workspace ('release/' + $versions[0] + '.md')) -Destination (Join-Path $release 'RELEASE_NOTES.md')
& (Join-Path $PSScriptRoot 'Test-PublicationSafety.ps1')
Write-Output "Prepared $($pending.Count) VPM ZIPs and docs/index.json. No upload performed."
Write-Output "Publish tag: v$($versions[0])"
Write-Output "VCC repository URL: $ListingUrl"
Write-Output ('Add to VCC: vcc://vpm/addRepo?url=' + [Uri]::EscapeDataString($ListingUrl))
