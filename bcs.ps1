# Runs the BevyCSharp command line out of this checkout, building it once if it is not there.
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$binary = Join-Path $root 'BevyCSharp.Cli\bin\Debug\net10.0\bcs.exe'

# Built with no node kept for reuse, since MSBuild's worker nodes start with the standard streams of
# whatever ran this and idle on after the build waiting to be used again, so a caller reading this
# tool's output would read until they ended rather than until this did.
if (-not (Test-Path $binary)) {
    dotnet build (Join-Path $root 'BevyCSharp.Cli\BevyCSharp.Cli.csproj') -v q --nologo -nodeReuse:false
}

& $binary @args
exit $LASTEXITCODE
