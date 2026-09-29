#Requires -Version 7.4
param([switch]$SourceOnly)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ReleaseCommon.ps1')
$workspace = Split-Path -Parent $PSScriptRoot
$problems = [Collections.Generic.List[string]]::new()
$script:checked = 0
# Findings report paths and rule names only; never print a matched secret.
$rules = [ordered]@{
    'private key' = '-----BEGIN (?:RSA |EC |DSA |OPENSSH |ENCRYPTED )?PRIVATE KEY-----'
    'GitHub token' = '(?:gh[pousr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{50,})'
    'AWS access key' = '(?:AKIA|ASIA)[A-Z0-9]{16}'
    'Slack token' = 'xox[baprs]-[A-Za-z0-9-]{20,}'
    'API key' = '(?:sk-(?:proj-|svcacct-)?[A-Za-z0-9_-]{32,}|AIza[A-Za-z0-9_-]{35})'
    'credential URL' = 'https?://[^\s/:]+:[^\s/@]+@'
    'local user path' = '(?i)(?:[A-Z]:[/\\]Users[/\\](?!<|user(?:Name)?[/\\]|example[/\\])[^\s/\\]+|/[U]sers/(?!example/)[^\s/]+)'
    'literal credential assignment' = '(?im)^\s*["'']?(?:api[_-]?key|access[_-]?token|client[_-]?secret|password)["'']?\s*[:=]\s*["''][A-Za-z0-9+/=_-]{16,}["'']'
}
function Test-PublicText([string]$Name, [string]$Text) {
    $script:checked++
    foreach ($rule in $rules.GetEnumerator()) {
        if ($Text -match $rule.Value) { $problems.Add("$Name : $($rule.Key)") }
    }
}
function Read-StreamText([IO.Stream]$Stream) {
    $reader = [IO.StreamReader]::new($Stream, [Text.Encoding]::UTF8, $true, 1024, $true)
    try { $reader.ReadToEnd() } finally { $reader.Dispose() }
}
$sourceFiles = @()
foreach ($name in @('Packages','scripts','docs','design','release')) {
    $dir = Join-Path $workspace $name
    Assert-VerificationPath $dir $workspace
    Assert-NoVerificationLinks $dir
    $sourceFiles += Get-ChildItem -LiteralPath $dir -File -Recurse | Where-Object { $_.FullName -notmatch '[/\\](?:_site|vendor|\.jekyll-cache|\.bundle)[/\\]' }
}
$sourceFiles += Get-ChildItem -LiteralPath $workspace -File -Force | Where-Object { $_.Extension -ne '.docx' }
foreach ($file in $sourceFiles) {
    $relative = [IO.Path]::GetRelativePath($workspace, $file.FullName)
    if ($file.Name -match '^(?:\.env(?:\..*)?|credentials(?:\..*)?)$' -or $file.Extension -in @('.pem','.key','.pfx','.p12','.keystore','.jks')) {
        $problems.Add("$relative : forbidden credential file")
    }
    Test-PublicText $relative ([IO.File]::ReadAllText($file.FullName))
}
if (Test-Path -LiteralPath (Join-Path $workspace '.git')) {
    $tracked = @(& git -C $workspace ls-files)
    if ($LASTEXITCODE -ne 0) { throw 'Could not inspect tracked files.' }
    foreach ($path in $tracked) {
        if ($path -match '(?i)(^|/)(\.verification~|artifacts|Library|Logs|UserSettings|\.codex|\.agents|\.env(?:\.[^/]*)?)(/|$)|\.(docx|pem|key|pfx|p12|keystore|jks)$') {
            $problems.Add("$path : forbidden tracked file")
        }
    }
}
if (-not $SourceOnly) {
    foreach ($archive in Get-ChildItem -LiteralPath (Join-Path $workspace 'artifacts') -File -Recurse | Where-Object { $_.Extension -in @('.zip','.unitypackage') }) {
        Assert-VerificationPath $archive.FullName $workspace
        if ($archive.Name -notmatch '^lazyFade-(lilToon|NonToon)-([0-9]+\.[0-9]+\.[0-9]+)\.(zip|unitypackage)$') { throw 'Unexpected release archive name.' }
        $edition = $Matches[1]; $version = $Matches[2]
        $source = Join-Path $workspace ('Packages/com.camellian.lazyfade.' + $edition.ToLowerInvariant())
        $info = Get-Content -LiteralPath (Join-Path $source 'package.json') -Raw | ConvertFrom-Json
        # Old versions remain immutable; scan all payloads, compare current version with source.
        $isCurrent = $version -eq $info.version
        $allowed = @{}
        foreach ($file in Get-ReleaseFiles $source -UnityPackage:($archive.Extension -eq '.unitypackage')) {
            $relative = [IO.Path]::GetRelativePath($source, $file.FullName).Replace('\','/')
            $allowed[$relative] = [IO.File]::ReadAllText($file.FullName)
        }
        if ($archive.Extension -eq '.zip') {
            $zipMemory = [IO.MemoryStream]::new([IO.File]::ReadAllBytes($archive.FullName), $false)
            $zip = [IO.Compression.ZipArchive]::new($zipMemory, [IO.Compression.ZipArchiveMode]::Read, $false)
            try {
                $seen = @{}
                foreach ($entry in $zip.Entries) {
                    if ($seen.ContainsKey($entry.FullName) -or -not $allowed.ContainsKey($entry.FullName)) { throw 'Unexpected or duplicate ZIP entry.' }
                    $seen[$entry.FullName] = $true
                    $stream = $entry.Open()
                    try { $value = Read-StreamText $stream } finally { $stream.Dispose() }
                    Test-PublicText ($archive.Name + '/' + $entry.FullName) $value
                    if ($isCurrent -and $entry.FullName -ne 'package.json' -and $value -cne $allowed[$entry.FullName]) { throw 'ZIP content differs from current source.' }
                    if ($entry.FullName -eq 'package.json') {
                        $manifest = $value | ConvertFrom-Json
                        if ($manifest.name -cne $info.name -or $manifest.version -cne $version) { throw 'ZIP manifest mismatch.' }
                        if ($isCurrent) {
                            foreach ($property in $info.PSObject.Properties) {
                                if ($property.Name -eq 'author') {
                                    if ($manifest.author.name -cne $info.author.name) { throw 'ZIP author mismatch.' }
                                } elseif (($manifest.($property.Name) | ConvertTo-Json -Depth 30 -Compress) -cne ($property.Value | ConvertTo-Json -Depth 30 -Compress)) {
                                    throw 'ZIP manifest differs from source.'
                                }
                            }
                            $download = [Uri]$manifest.url
                            if (-not $download.IsAbsoluteUri -or $download.Scheme -ne 'https' -or $download.UserInfo) { throw 'Unsafe ZIP download URL.' }
                        }
                    }
                }
                if ($isCurrent -and $seen.Count -ne $allowed.Count) { throw 'Incomplete ZIP.' }
            } finally { $zip.Dispose(); $zipMemory.Dispose() }
        } else {
            $fileStream = [IO.File]::OpenRead($archive.FullName)
            $gzip = [IO.Compression.GZipStream]::new($fileStream, [IO.Compression.CompressionMode]::Decompress)
            $tar = [System.Formats.Tar.TarReader]::new($gzip)
            $records = @{}
            try {
                while ($null -ne ($entry = $tar.GetNextEntry())) {
                    if ($entry.EntryType -eq [System.Formats.Tar.TarEntryType]::Directory) { continue }
                    if ($entry.Name -notmatch '^([a-f0-9]{32})/(asset|asset.meta|pathname|preview.png)$' -or $entry.EntryType -notin @('RegularFile','V7RegularFile')) { throw 'Unexpected Unity archive entry.' }
                    $guid = $Matches[1]; $part = $Matches[2]
                    if (-not $records.ContainsKey($guid)) { $records[$guid] = @{} }
                    if ($records[$guid].ContainsKey($part)) { throw 'Duplicate Unity archive entry.' }
                    $value = if ($part -eq 'preview.png') { '' } else { Read-StreamText $entry.DataStream }
                    $records[$guid][$part] = $value
                    if ($part -ne 'preview.png') { Test-PublicText ($archive.Name + '/' + $guid + '/' + $part) $value }
                }
            } finally { $tar.Dispose(); $gzip.Dispose(); $fileStream.Dispose() }
            $seen = @{}
            foreach ($record in $records.Values) {
                $path = ([string]$record.pathname).TrimEnd("`r","`n")
                $prefix = 'Assets/lazyFade-' + $edition
                if ($path -eq $prefix) { continue }
                if (-not $path.StartsWith($prefix + '/', [StringComparison]::Ordinal)) { throw 'Unity asset outside release folder.' }
                $relative = $path.Substring($prefix.Length + 1)
                foreach ($part in @('asset','asset.meta')) {
                    if (-not $record.ContainsKey($part)) { continue }
                    $key = if ($part -eq 'asset.meta') { $relative + '.meta' } else { $relative }
                    if (-not $allowed.ContainsKey($key) -or $seen.ContainsKey($key)) { throw "Unexpected Unity asset: $key" }
                    $seen[$key] = $true
                    if ($isCurrent -and $record[$part] -cne $allowed[$key]) { throw "Unity content differs from source: $key" }
                }
            }
            if ($isCurrent -and $seen.Count -ne $allowed.Count) { throw 'Incomplete Unity archive.' }
        }
    }
    foreach ($directory in Get-ChildItem -LiteralPath (Join-Path $workspace 'artifacts') -Directory) {
        $sumsPath = Join-Path $directory.FullName 'SHA256SUMS.txt'
        Assert-VerificationPath $sumsPath $workspace
        if (-not (Test-Path -LiteralPath $sumsPath)) { throw 'Release checksum file is missing.' }
        $sums = @{}
        foreach ($line in Get-Content -LiteralPath $sumsPath) {
            if ($line -notmatch '^([a-f0-9]{64})  (lazyFade-(?:lilToon|NonToon)-[0-9]+\.[0-9]+\.[0-9]+\.(?:zip|unitypackage))$') { throw 'Malformed checksum line.' }
            if ($sums.ContainsKey($Matches[2])) { throw 'Duplicate checksum entry.' }
            $sums[$Matches[2]] = $Matches[1]
        }
        $archives = @(Get-ChildItem -LiteralPath $directory.FullName -File | Where-Object { $_.Extension -in @('.zip','.unitypackage') })
        if ($sums.Count -ne $archives.Count) { throw 'Checksum count mismatch.' }
        foreach ($file in $archives) {
            if ($sums[$file.Name] -cne (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()) { throw 'Release checksum mismatch.' }
        }
        foreach ($file in Get-ChildItem -LiteralPath $directory.FullName -File | Where-Object { $_.Extension -notin @('.zip','.unitypackage') }) {
            if ($file.Name -notin @('SHA256SUMS.txt','RELEASE_NOTES.md')) { throw 'Unexpected release sidecar.' }
            Test-PublicText ('artifacts/' + $file.Name) ([IO.File]::ReadAllText($file.FullName))
        }
    }
}
if ($problems.Count) {
    $problems | ForEach-Object { Write-Output $_ }
    throw "Publication check failed: $($problems.Count) finding(s). Values suppressed."
}
Write-Output "Publication safety passed: $script:checked text payloads checked. Pattern checks are not a guarantee; Git history and public hosting require separate review."
