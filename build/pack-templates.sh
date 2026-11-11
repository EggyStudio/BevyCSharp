#!/usr/bin/env bash
# Packs the templates `dotnet new` starts a game from, templates/, beside the engine's package in
# build/package, at the engine's version and asking for it, so a game made from a template builds
# on the package packed with it.
#
#   dotnet pack BevyCSharp/BevyCSharp.csproj -c Release    # into build/package
#   build/pack-templates.sh [version]
#
# The version is the engine package's packed last unless one is named. The templates are packed from
# a copy in build/templates with the version written into each template.json, where each template
# itself holds a placeholder, since a template engine fills in what a project asks for and nothing
# fills in the template. The copy is inside the repository, so Directory.Build.props gives the
# package the repository's license, icon and addresses.
set -euo pipefail
cd "$(dirname "$0")/.."

version="${1:-$(ls -t build/package/BevyCSharp.[0-9]*.nupkg | head -1 | sed 's/.*BevyCSharp\.\([0-9].*\)\.nupkg/\1/')}"
[ -n "$version" ] || { echo "no engine package in build/package to take a version from, and none named" >&2; exit 1; }

rm -rf build/templates
cp -r templates build/templates
# Written beside each and moved over it, since the sed of macOS reads -i otherwise than GNU's.
for template in build/templates/content/*/.template.config/template.json; do
    sed "s/\"PACKED_VERSION\"/\"$version\"/" "$template" > "$template.new" && mv "$template.new" "$template"
    grep -q "\"$version\"" "$template" || { echo "the version was not written into $template" >&2; exit 1; }
done

dotnet pack build/templates/BevyCSharp.Templates.csproj -c Release -p:Version="$version" -o build/package
echo "packed BevyCSharp.Templates.$version.nupkg, its templates asking for BevyCSharp $version"
