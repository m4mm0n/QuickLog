<#
Header updater for ALL C# source files in the repo (PowerShell 5.1+ compatible)

Created timestamp source:
- If header exists: keep Created from header.
- If header missing:
    - Prefer git "added" commit timestamp (first commit adding the file).
    - Fallback to filesystem CreationTime for new/uncommitted files.

Other behavior:
- Scans ALL tracked files matching configured extensions (default: .cs) via `git ls-files`.
- Inserts header if missing (+ // CRC32-BODY marker).
- CRC32 is computed on the BODY only (content after header + optional CRC32-BODY marker).
- If body CRC differs from stored CRC: updates Last Modified, CRC32, CRC32-BODY, and Description (from /// <summary> if found).
- Re-stages modified files automatically.
- Handles UTF-8 BOM and removes accidental duplicate headers at the start of the body.
#>

param(
    [string] $ConfigPath = ".headerconfig.json"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Invoke-Git {
    param(
        [Parameter(Mandatory = $true)]
        [string[]] $GitArgs
    )

    $output = & git @GitArgs 2>&1
    $code = $LASTEXITCODE

    if ($code -ne 0) {
        $joined = ($output | ForEach-Object { "$_" }) -join "`r`n"
        throw ("git failed (exit {0}): git {1}`r`n{2}" -f $code, ($GitArgs -join " "), $joined)
    }

    return ($output -join "`r`n")
}

function Remove-LeadingBomChar {
    param([string] $Text)
    if ($Text.Length -gt 0 -and $Text[0] -eq [char]0xFEFF) {
        return $Text.Substring(1)
    }
    return $Text
}

function Trim-BodyStart {
    param([string] $Text)

    $t = $Text
    $t = $t -replace "^\uFEFF+", ""
    $t = [System.Text.RegularExpressions.Regex]::Replace($t, "^\s+", "", 1)
    return $t
}

function Get-Crc32Hex {
    param([byte[]] $Data)

    $hex  = [System.Globalization.NumberStyles]::HexNumber
    $poly = [uint32]::Parse("EDB88320", $hex)
    $mask = [uint32]::Parse("FFFFFFFF", $hex)

    $table = New-Object uint32[] 256

    for ($i = 0; $i -lt 256; $i++) {
        $c = [uint32]$i
        for ($k = 0; $k -lt 8; $k++) {
            if (($c -band 1) -ne 0) { $c = $poly -bxor ($c -shr 1) }
            else { $c = $c -shr 1 }
        }
        $table[$i] = $c
    }

    $crc = $mask
    foreach ($b in $Data) {
        $idx = [byte](($crc -bxor $b) -band 0xFF)
        $crc = $table[$idx] -bxor ($crc -shr 8)
    }

    $crc = $crc -bxor $mask
    return $crc.ToString("X8")
}

function Detect-Newline {
    param([string] $Text)
    if ($Text -like "*`r`n*") { return "`r`n" }
    return "`n"
}

function Extract-SummaryLines {
    param([string] $Body)

    $options = [System.Text.RegularExpressions.RegexOptions]::Multiline -bor
               [System.Text.RegularExpressions.RegexOptions]::Singleline

    $m = [System.Text.RegularExpressions.Regex]::Match(
        $Body,
        '^\s*///\s*<summary>\s*(?<inner>.*?)^\s*///\s*</summary>\s*',
        $options
    )

    if (-not $m.Success) { return @() }

    $inner = $m.Groups["inner"].Value
    $lines = $inner -split "\r?\n" | ForEach-Object {
        ($_ -replace '^\s*///\s?', '').Trim()
    } | Where-Object { $_ -ne "" }

    return ,$lines
}

function Build-Header {
    param(
        [string]   $ProjectName,
        [string]   $FileName,
        [string]   $Author,
        [string]   $Created,
        [string]   $LastModified,
        [string]   $Crc32,
        [string[]] $DescriptionLines,
        [string]   $LicenseName,
        [string]   $LicenseUrl,
        [string]   $Notes,
        [string]   $NL
    )

    if (-not $DescriptionLines) { $DescriptionLines = @() }

    $descBlock = if ($DescriptionLines.Count -gt 0) {
        ($DescriptionLines | ForEach-Object { " *                   $($_)" }) -join $NL
    } else {
        " *"
    }

    $header = @"
/*
 * ====================================================================================================
 *  Project        : $ProjectName
 *  File           : $FileName
 *  Author         : $Author
 *  Created        : $Created
 *  Last Modified  : $LastModified
 *  CRC32          : $Crc32
 *  
 *  Description    :
$descBlock
 * 
 *  License        :
 *                   $LicenseName
 *                   $LicenseUrl
 *
 *  Notes          :
 *                   $Notes
 * ====================================================================================================
 */
"@

    return ($header -replace "`r`n", $NL)
}

function Try-Extract-ExistingHeader {
    param(
        [string] $Text,
        [ref]    $Header,
        [ref]    $AfterHeader,
        [ref]    $StoredHash,
        [ref]    $CreatedValue
    )

    $Header.Value       = $null
    $AfterHeader.Value  = $Text
    $StoredHash.Value   = $null
    $CreatedValue.Value = $null

    $t = $Text -replace "^\uFEFF+", ""
    $m = [System.Text.RegularExpressions.Regex]::Match($t, '(?s)\A\s*(?<hdr>/\*.*?\*/)\s*\r?\n')
    if (-not $m.Success) { return $false }

    $hdr = $m.Groups["hdr"].Value

    if ($hdr -notmatch '={10,}' -or $hdr -notmatch '\bProject\b' -or $hdr -notmatch '\bCRC32\b') { return $false }

    $Header.Value = $hdr
    $rest = $t.Substring($m.Length)

    $m2 = [System.Text.RegularExpressions.Regex]::Match($rest, '^\s*//\s*CRC32-BODY\s*:\s*(?<h>[0-9A-Fa-f]{8})\s*\r?\n')
    if ($m2.Success) {
        $StoredHash.Value = $m2.Groups["h"].Value.ToUpperInvariant()
        $rest = $rest.Substring($m2.Length)
        $rest = [System.Text.RegularExpressions.Regex]::Replace($rest, '^\s*\r?\n', '', 1)
    } else {
        $mCrc = [System.Text.RegularExpressions.Regex]::Match($hdr, '(?m)^\s*\*\s*CRC32\s*:\s*(?<h>[0-9A-Fa-f]{8})\s*$')
        if ($mCrc.Success) { $StoredHash.Value = $mCrc.Groups["h"].Value.ToUpperInvariant() }
    }

    $mCreated = [System.Text.RegularExpressions.Regex]::Match($hdr, '(?m)^\s*\*\s*Created\s*:\s*(?<c>.+?)\s*$')
    if ($mCreated.Success) { $CreatedValue.Value = $mCreated.Groups["c"].Value.Trim() }

    $AfterHeader.Value = $rest
    return $true
}

function Strip-DuplicateLeadingHeaders {
    param([string] $Body)

    $current = $Body
    while ($true) {
        $h = $null; $rest = $null; $sh = $null; $cv = $null
        $ok = Try-Extract-ExistingHeader -Text $current -Header ([ref]$h) -AfterHeader ([ref]$rest) -StoredHash ([ref]$sh) -CreatedValue ([ref]$cv)
        if (-not $ok) { break }
        $current = $rest
    }
    return $current
}

function Format-DateTimeOffset {
    param([DateTimeOffset] $Dto)
    return $Dto.ToString("yyyy-MM-dd HH:mm:ss zzz")
}

function Get-CreatedForNewHeader {
    param(
        [string] $RelPath,
        [string] $FullPath
    )

    # 1) Git "added" commit date (best approximation of "created" in a repo context)
    # %aI is strict ISO 8601 (author date). We use author date to reflect when it was written.
    try {
        $iso = (Invoke-Git -GitArgs @("log","--follow","--diff-filter=A","--format=%aI","-1","--",$RelPath)).Trim()
        if ($iso) {
            $dto = [DateTimeOffset]::Parse($iso)
            return (Format-DateTimeOffset -Dto $dto)
        }
    } catch {
        # ignore and fall back
    }

    # 2) Filesystem creation time (for uncommitted/new files)
    $item = Get-Item -LiteralPath $FullPath -ErrorAction Stop
    $dto2 = New-Object DateTimeOffset($item.CreationTime)
    return (Format-DateTimeOffset -Dto $dto2)
}

# -------------------- Main --------------------

$repoRoot = (Invoke-Git -GitArgs @("rev-parse","--show-toplevel")).Trim()
if (-not $repoRoot) { throw "Not in a git repo." }
Set-Location $repoRoot

if (-not (Test-Path $ConfigPath)) {
    throw "Missing config file: $ConfigPath (expected at repo root unless you pass -ConfigPath)"
}

$config = Get-Content -Raw -Path $ConfigPath | ConvertFrom-Json
$projectName = [string]$config.projectName
$author      = [string]$config.author
$notes       = [string]$config.notes
$licenseName = [string]$config.license.name
$licenseUrl  = [string]$config.license.url

$extensions = @()
if ($config.extensions) { $extensions = @($config.extensions | ForEach-Object { [string]$_ }) }
if ($extensions.Count -eq 0) { $extensions = @(".cs") }

# ALL source files: tracked/indexed, NUL separated
$patterns = @()
foreach ($e in $extensions) {
    $ext = $e
    if (-not $ext.StartsWith(".")) { $ext = "." + $ext }
    $patterns += ("*" + $ext)
}

$raw = Invoke-Git -GitArgs (@("ls-files","-z","--") + $patterns)
$paths = $raw -split "`0" | Where-Object { $_ -and $_.Trim() -ne "" }
if ($paths.Count -eq 0) { exit 0 }

$nowDto = [DateTimeOffset]::Now
$nowStr = Format-DateTimeOffset -Dto $nowDto

# cache created lookups (git log can be expensive on large repos)
$createdCache = @{}

foreach ($rel in $paths) {
    $ext = [System.IO.Path]::GetExtension($rel)
    if (-not ($extensions -contains $ext)) {
        if (-not ($extensions -contains ($ext.ToLowerInvariant()))) { continue }
    }

    $full = Join-Path $repoRoot $rel
    if (-not (Test-Path $full)) { continue }

    $bytes = [System.IO.File]::ReadAllBytes($full)
    if ($bytes -contains 0) { continue } # binary

    $hasBom = ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
    $enc = if ($hasBom) { New-Object System.Text.UTF8Encoding($true) } else { New-Object System.Text.UTF8Encoding($false) }

    $text = $enc.GetString($bytes)
    $text = Remove-LeadingBomChar -Text $text

    $nl = Detect-Newline -Text $text

    $existingHeader = $null
    $afterHeader    = $null
    $storedHash     = $null
    $createdVal     = $null

    $hasHeader = Try-Extract-ExistingHeader `
        -Text $text `
        -Header ([ref]$existingHeader) `
        -AfterHeader ([ref]$afterHeader) `
        -StoredHash ([ref]$storedHash) `
        -CreatedValue ([ref]$createdVal)

    if (-not $hasHeader) {
        $body = Strip-DuplicateLeadingHeaders -Body $text
        $body = Trim-BodyStart -Text $body

        $descLines = Extract-SummaryLines -Body $body
        $crc = Get-Crc32Hex -Data ($enc.GetBytes($body))

        if (-not $createdCache.ContainsKey($rel)) {
            $createdCache[$rel] = Get-CreatedForNewHeader -RelPath $rel -FullPath $full
        }
        $createdStr = [string]$createdCache[$rel]

        $hdr = Build-Header `
            -ProjectName $projectName `
            -FileName ([System.IO.Path]::GetFileName($rel)) `
            -Author $author `
            -Created $createdStr `
            -LastModified $nowStr `
            -Crc32 $crc `
            -DescriptionLines $descLines `
            -LicenseName $licenseName `
            -LicenseUrl $licenseUrl `
            -Notes $notes `
            -NL $nl

        $newText = $hdr + $nl + "// CRC32-BODY: $crc" + $nl + $nl + $body
        [System.IO.File]::WriteAllText($full, $newText, $enc)

        Invoke-Git -GitArgs @("add","--",$rel) | Out-Null
        continue
    }

    # Header exists: strip accidental nested headers from body
    $bodyText = Strip-DuplicateLeadingHeaders -Body $afterHeader
    $bodyText = Trim-BodyStart -Text $bodyText

    $currentCrc = Get-Crc32Hex -Data ($enc.GetBytes($bodyText))
    $stored = if ($storedHash) { $storedHash } else { "" }

    if ($currentCrc -eq $stored) {
        continue
    }

    $createdOut = if ($createdVal) { $createdVal } else { $nowStr }
    $descLines2 = Extract-SummaryLines -Body $bodyText

    $hdr2 = Build-Header `
        -ProjectName $projectName `
        -FileName ([System.IO.Path]::GetFileName($rel)) `
        -Author $author `
        -Created $createdOut `
        -LastModified $nowStr `
        -Crc32 $currentCrc `
        -DescriptionLines $descLines2 `
        -LicenseName $licenseName `
        -LicenseUrl $licenseUrl `
        -Notes $notes `
        -NL $nl

    $newText2 = $hdr2 + $nl + "// CRC32-BODY: $currentCrc" + $nl + $nl + $bodyText
    [System.IO.File]::WriteAllText($full, $newText2, $enc)

    Invoke-Git -GitArgs @("add","--",$rel) | Out-Null
}

exit 0