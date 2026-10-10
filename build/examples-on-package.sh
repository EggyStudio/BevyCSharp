#!/usr/bin/env bash
# Builds every example on the packed package alone, in a project of its own outside the repository,
# as a reader who copies one into a game of their own does (NORM.md, N 2.7). A picture in the README
# opens an example as the way to do a thing, so what an example calls has to be what a game on the
# package can call, its generator and its build files included, and an example that leans on the
# examples project, or on what the library keeps to itself, fails here.
#
#   build/examples-on-package.sh <version>
#
# The package is the version named, packed into build/package beforehand, as the workflow packs one
# for its run.
set -euo pipefail

version="$1"
cd "$(dirname "$0")/.."
root="$PWD"

work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT
mkdir -p "$work/Examples"

# The sources alone, without what a build left or the assets an example loads as it runs.
(cd BevyCSharp.Examples && find . -name '*.cs' -not -path './bin/*' -not -path './obj/*' -print0 | tar --null -cf - --files-from=-) \
  | (cd "$work/Examples" && tar -xf -)

# Without what runs the examples, which a reader copying one into a game has none of: the catalog,
# the program that opens one by name, the record it keeps them in and the input a capture pretends,
# so an example reaching into any of them fails to build here.
rm "$work/Examples/Catalog.cs" "$work/Examples/Program.cs" "$work/Examples/Example.cs" "$work/Examples/Drives.cs"

# The helpers an example may still lean on, each with why, which only gets shorter. Each stands in
# for something Bevy's examples take from a crate the bridge does not compile in, and goes when it
# can be asked for.
#   FreeCamera.cs  the free camera of Bevy's camera controller crate
#   Widgets.cs     the radio buttons Bevy's examples share by path, which Feathers' replace

# What the repository's Directory.Build.props sets that the examples read, written into their own
# project, which takes the package in place of the library's project and its generator.
cat > "$work/Examples/BevyCSharp.Examples.csproj" <<EOF
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <OutputType>Library</OutputType>
        <TargetFramework>net10.0</TargetFramework>
        <RootNamespace>BevyCSharp.Examples</RootNamespace>
        <LangVersion>latest</LangVersion>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
    </PropertyGroup>
    <ItemGroup>
        <PackageReference Include="BevyCSharp" Version="$version" />
    </ItemGroup>
</Project>
EOF

cat > "$work/Examples/nuget.config" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="$root/build/package" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="local">
      <package pattern="BevyCSharp" />
    </packageSource>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
EOF

dotnet build "$work/Examples"
echo "every example builds on the package alone, beside the two helpers listed"
