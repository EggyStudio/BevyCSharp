# Runs the BevyCSharp command line out of this checkout, building it once if it is not there.
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$binary = Join-Path $root 'BevyCSharp.Cli\bin\Debug\net10.0\bcs.exe'

if (-not (Test-Path $binary)) {
    dotnet build (Join-Path $root 'BevyCSharp.Cli\BevyCSharp.Cli.csproj') -v q --nologo
}

& $binary @args
exit $LASTEXITCODE
