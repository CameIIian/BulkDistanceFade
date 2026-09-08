# Shared guards for the local verification helpers. No filesystem changes here.
function Assert-PackageName([string]$Name) {
    if ($Name -cnotmatch '\A[a-z0-9][a-z0-9_-]*(\.[a-z0-9][a-z0-9_-]*)+\z') {
        throw 'Invalid dependency package name.'
    }
}

function Assert-VerificationPath([string]$Path, [string]$Root) {
    $fullPath = [IO.Path]::GetFullPath($Path)
    $fullRoot = [IO.Path]::GetFullPath($Root).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    $prefix = $fullRoot + [IO.Path]::DirectorySeparatorChar
    if (-not $fullPath.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Verification path must remain inside its expected root.'
    }
    # Check existing ancestors as well as the target: a junction can bypass a
    # lexical containment check. Reject links instead of following them.
    $current = $fullPath
    while ($current) {
        $item = Get-Item -LiteralPath $current -Force -ErrorAction SilentlyContinue
        if ($null -ne $item -and ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw 'Symbolic links and junctions are not supported by verification helpers.'
        }
        $current = [IO.Path]::GetDirectoryName($current)
    }
}

function Assert-NoVerificationLinks([string]$Directory) {
    $pending = New-Object 'System.Collections.Generic.Stack[string]'
    $pending.Push($Directory)
    while ($pending.Count -gt 0) {
        foreach ($item in Get-ChildItem -LiteralPath $pending.Pop() -Force) {
            if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) {
                throw 'A verification dependency contains a symbolic link or junction.'
            }
            if ($item.PSIsContainer) { $pending.Push($item.FullName) }
        }
    }
}
