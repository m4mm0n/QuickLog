[CmdletBinding()]
param(
    [string]$PackageDirectory,
    [string]$WorkingDirectory
)

$ErrorActionPreference = 'Stop'
$PackageDirectory = if ([string]::IsNullOrWhiteSpace($PackageDirectory)) {
    Join-Path $PSScriptRoot '..\artifacts\nupkgs'
} else { $PackageDirectory }
$WorkingDirectory = if ([string]::IsNullOrWhiteSpace($WorkingDirectory)) {
    Join-Path $PSScriptRoot '..\artifacts\package-consumer'
} else { $WorkingDirectory }
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$artifactsRoot = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot 'artifacts'))
$packageRoot = [System.IO.Path]::GetFullPath($PackageDirectory)
$consumerRoot = [System.IO.Path]::GetFullPath($WorkingDirectory)

if (-not $consumerRoot.StartsWith($artifactsRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "WorkingDirectory must stay below $artifactsRoot"
}

$corePackage = Join-Path $packageRoot 'ZLS.QuickLog.3.0.0.nupkg'
$adapterPackage = Join-Path $packageRoot 'ZLS.QuickLog.Extensions.Logging.3.0.0.nupkg'
$coreSymbols = Join-Path $packageRoot 'ZLS.QuickLog.3.0.0.snupkg'
$adapterSymbols = Join-Path $packageRoot 'ZLS.QuickLog.Extensions.Logging.3.0.0.snupkg'
foreach ($required in @($corePackage, $adapterPackage, $coreSymbols, $adapterSymbols)) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) {
        throw "Missing package artifact: $required"
    }
}

Add-Type -AssemblyName System.IO.Compression.FileSystem

function Get-PackageEntries([string]$Path) {
    $archive = [System.IO.Compression.ZipFile]::OpenRead($Path)
    try {
        return @($archive.Entries | ForEach-Object FullName)
    }
    finally {
        $archive.Dispose()
    }
}

function Get-NuspecText([string]$Path) {
    $archive = [System.IO.Compression.ZipFile]::OpenRead($Path)
    try {
        $entry = $archive.Entries | Where-Object FullName -Like '*.nuspec' | Select-Object -First 1
        if ($null -eq $entry) { throw "Package has no nuspec: $Path" }
        $reader = [System.IO.StreamReader]::new($entry.Open())
        try { return $reader.ReadToEnd() }
        finally { $reader.Dispose() }
    }
    finally {
        $archive.Dispose()
    }
}

$coreEntries = Get-PackageEntries $corePackage
$adapterEntries = Get-PackageEntries $adapterPackage
$requiredCoreEntries = @(
    'lib/net8.0/QuickLog.dll',
    'lib/net8.0/QuickLog.xml',
    'lib/net10.0/QuickLog.dll',
    'lib/net10.0/QuickLog.xml',
    'README.md',
    'CHANGELOG.md',
    'LICENSE'
)
foreach ($entry in $requiredCoreEntries) {
    if ($entry -notin $coreEntries) { throw "Core package is missing $entry" }
}

$requiredAdapterEntries = @(
    'lib/net8.0/QuickLog.Extensions.Logging.dll',
    'lib/net8.0/QuickLog.Extensions.Logging.xml',
    'lib/net10.0/QuickLog.Extensions.Logging.dll',
    'lib/net10.0/QuickLog.Extensions.Logging.xml'
)
foreach ($entry in $requiredAdapterEntries) {
    if ($entry -notin $adapterEntries) { throw "Adapter package is missing $entry" }
}

$coreNuspec = Get-NuspecText $corePackage
if ($coreNuspec -match '<dependency\s') {
    throw 'ZLS.QuickLog must remain dependency-free.'
}
$adapterNuspec = Get-NuspecText $adapterPackage
if ($adapterNuspec -notmatch 'ZLS\.QuickLog' -or $adapterNuspec -notmatch 'Microsoft\.Extensions\.Logging') {
    throw 'The adapter package does not declare its expected dependencies.'
}

if (Test-Path -LiteralPath $consumerRoot) {
    Remove-Item -LiteralPath $consumerRoot -Recurse -Force
}
[System.IO.Directory]::CreateDirectory($consumerRoot) | Out-Null

$project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFrameworks>net8.0;net10.0</TargetFrameworks>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="ZLS.QuickLog" Version="3.0.0" />
    <PackageReference Include="ZLS.QuickLog.Extensions.Logging" Version="3.0.0" />
  </ItemGroup>
</Project>
"@
[System.IO.File]::WriteAllText((Join-Path $consumerRoot 'Consumer.csproj'), $project)

$program = @"
using Microsoft.Extensions.Logging;
using QuickLog;
using QuickLog.Extensions.Logging;
using QuickLog.Loggers;
using QuickLog.Utilities;

var root = Path.Combine(Path.GetTempPath(), $"quicklog-package-{Guid.NewGuid():N}");
Directory.CreateDirectory(root);
var qlog = Path.Combine(root, "consumer.qlog");
await using var quickLogger = new QuickLogger
{
    EnableAsyncLogging = true,
    AsyncOnly = true,
    EnableAsyncBinaryLogging = true,
    BinaryLogPath = qlog
};
using var factory = LoggerFactory.Create(builder => builder.ClearProviders().AddQuickLog(quickLogger));
factory.CreateLogger("PackageConsumer").LogWarning(new EventId(3300, "PackageEvent"), "Value {Value}", 42);
await quickLogger.ShutdownAsync(TimeSpan.FromSeconds(5));
var entry = BinaryLogReader.Read(qlog).Single();
if (entry.EventId.Id != 3300 || !entry.Properties!.ContainsKey("Value"))
    throw new InvalidOperationException("Package consumer lost structured data.");
Console.WriteLine("PACKAGE_CONSUMER_OK");
Directory.Delete(root, true);
"@
[System.IO.File]::WriteAllText((Join-Path $consumerRoot 'Program.cs'), $program)

$escapedPackageRoot = [System.Security.SecurityElement]::Escape($packageRoot)
$nugetConfig = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="$escapedPackageRoot" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
"@
$nugetConfigPath = Join-Path $consumerRoot 'NuGet.Config'
[System.IO.File]::WriteAllText($nugetConfigPath, $nugetConfig)

dotnet restore (Join-Path $consumerRoot 'Consumer.csproj') --configfile $nugetConfigPath
if ($LASTEXITCODE -ne 0) { throw 'Package consumer restore failed.' }
dotnet build (Join-Path $consumerRoot 'Consumer.csproj') --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Package consumer build failed.' }
foreach ($framework in @('net8.0', 'net10.0')) {
    $output = dotnet run --project (Join-Path $consumerRoot 'Consumer.csproj') --framework $framework --configuration Release --no-build
    if ($LASTEXITCODE -ne 0 -or $output -notcontains 'PACKAGE_CONSUMER_OK') {
        throw "Package consumer runtime smoke failed for $framework`: $($output -join [Environment]::NewLine)"
    }
}

Write-Output 'PACKAGE_LAYOUT_OK'
Write-Output 'PACKAGE_DEPENDENCIES_OK'
Write-Output 'PACKAGE_CONSUMER_OK'
