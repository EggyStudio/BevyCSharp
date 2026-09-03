<#
.SYNOPSIS
    Builds the native Bevy bridge and stages it where the NuGet package expects it.

.DESCRIPTION
    The PowerShell twin of build-native.sh, for developers on Windows who would rather not go
    through WSL or Git Bash. Both scripts produce identical output, so either can be used on any
    platform PowerShell runs on.

    Everything generated lives under build/: cargo's target directory and the staged per-RID
    artifacts. The repository root stays clean.

.PARAMETER Render
    Build with Bevy's renderer and windowing instead of the headless profile. Takes several
    minutes the first time and produces a much larger library.

.PARAMETER Editor
    Build the render profile plus the HTML and CSS user interface, which is what
    BevyCSharp.Editor needs. Costs about a hundred crates over -Render.

.PARAMETER Target
    Rust target triple to build for. Defaults to the host.

.PARAMETER Clean
    Remove build/target and build/artifacts before building.

.EXAMPLE
    build/build-native.ps1
    build/build-native.ps1 -Render
    build/build-native.ps1 -Editor
    build/build-native.ps1 -Target aarch64-pc-windows-msvc -Render
#>
[CmdletBinding()]
param(
    [switch] $Render,
    [switch] $Editor,
    [string] $Target = '',
    [switch] $Clean
)

$ErrorActionPreference = 'Stop'

# This script's own directory. All build output is written here.
$BuildDir = $PSScriptRoot

# One level up, purely to locate the Rust sources. Nothing is ever written there.
$RepoRoot = Split-Path -Parent $BuildDir

$NativeDir = Join-Path $RepoRoot 'native'
$TargetDir = Join-Path $BuildDir 'target'
$ArtifactDir = Join-Path $BuildDir 'artifacts'
# -Editor implies the renderer, so it wins when both are given rather than being refused.
$Features = if ($Editor) { 'editor' } elseif ($Render) { 'render' } else { 'headless' }

if (-not (Test-Path (Join-Path $NativeDir 'Cargo.toml'))) {
    throw "No Rust workspace at '$NativeDir'. This script expects to live in <repo>/build/ with the sources in <repo>/native/."
}

if (-not (Get-Command cargo -ErrorAction SilentlyContinue)) {
    throw 'cargo not found. Install Rust from https://rustup.rs and re-run.'
}

if ($Clean) {
    Write-Host "==> cleaning $TargetDir and $ArtifactDir"
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue $TargetDir, $ArtifactDir
}

# Maps a Rust target triple onto the .NET runtime identifier and library file name that the
# NuGet runtimes/ layout keys off. Keep in step with rid_for_target in build-native.sh.
$RidMap = @{
    'x86_64-unknown-linux-gnu'   = @('linux-x64',        'libbevy_csharp.so')
    'aarch64-unknown-linux-gnu'  = @('linux-arm64',      'libbevy_csharp.so')
    'x86_64-unknown-linux-musl'  = @('linux-musl-x64',   'libbevy_csharp.so')
    'aarch64-unknown-linux-musl' = @('linux-musl-arm64', 'libbevy_csharp.so')
    'x86_64-pc-windows-msvc'     = @('win-x64',          'bevy_csharp.dll')
    'aarch64-pc-windows-msvc'    = @('win-arm64',        'bevy_csharp.dll')
    'x86_64-pc-windows-gnu'      = @('win-x64',          'bevy_csharp.dll')
    'x86_64-apple-darwin'        = @('osx-x64',          'libbevy_csharp.dylib')
    'aarch64-apple-darwin'       = @('osx-arm64',        'libbevy_csharp.dylib')
}

if ([string]::IsNullOrEmpty($Target)) {
    $Target = (rustc -vV | Select-String '^host:').ToString().Split(' ')[1]
}

if (-not $RidMap.ContainsKey($Target)) {
    throw "No .NET runtime identifier is mapped for target '$Target'. Add it to `$RidMap in $($MyInvocation.MyCommand.Name)."
}

$Rid, $LibName = $RidMap[$Target]

Write-Host '==> building bevy_csharp'
Write-Host "    sources  : $NativeDir"
Write-Host "    output   : $TargetDir"
Write-Host "    target   : $Target"
Write-Host "    rid      : $Rid"
Write-Host "    features : $Features"

if ($Render -or $Editor) {
    Write-Host "    note     : builds Bevy's renderer, winit and wgpu. This takes several minutes"
    Write-Host '               the first time and produces a much larger library.'
}

cargo build `
    --release `
    --manifest-path (Join-Path $NativeDir 'Cargo.toml') `
    --target-dir $TargetDir `
    --target $Target `
    --no-default-features `
    --features $Features

if ($LASTEXITCODE -ne 0) { throw "cargo build failed with exit code $LASTEXITCODE." }

$Built = Join-Path $TargetDir (Join-Path $Target (Join-Path 'release' $LibName))
if (-not (Test-Path $Built)) {
    throw "cargo reported success but '$Built' is missing."
}

# The per-RID slot the NuGet package picks up at pack time.
$RidDir = Join-Path $ArtifactDir $Rid
New-Item -ItemType Directory -Force -Path $RidDir | Out-Null
Copy-Item -Force $Built (Join-Path $RidDir $LibName)

# A flat copy for repo-local development: the projects copy this next to their own output, so
# running the sample or the tests works without packing first.
$FlatDir = Join-Path $TargetDir 'release'
New-Item -ItemType Directory -Force -Path $FlatDir | Out-Null
Copy-Item -Force $Built (Join-Path $FlatDir $LibName)

Write-Host "==> staged $(Join-Path $RidDir $LibName)"
Write-Host "==> staged $(Join-Path $FlatDir $LibName) (for local runs)"
