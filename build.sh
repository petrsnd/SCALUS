#!/usr/bin/env bash
#
# Local build entry point for macOS / Linux. Thin wrapper that replaces the old
# Cake build: it tests, publishes, then packages for the host (or --runtime) RID.
#
# Usage:
#   ./build.sh [--runtime osx-x64|linux-x64|...] [--configuration Release]
#              [--version 1.0.0] [--isrelease true|false] [--skip-tests]
set -euo pipefail

configuration="Release"
version="1.0.0"
runtime=""
isrelease="false"
skiptests="false"

while (( "$#" )); do
    case "$1" in
        --runtime)        runtime="$2";        shift 2 ;;
        --configuration)  configuration="$2";  shift 2 ;;
        --version)        version="$2";        shift 2 ;;
        --isrelease)      isrelease="$2";      shift 2 ;;
        --skip-tests)     skiptests="true";    shift ;;
        *) echo "Unknown argument: $1" >&2; exit 1 ;;
    esac
done

rootdir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$rootdir"

# Default the runtime to the host OS (x64) when not specified.
if [ -z "$runtime" ]; then
    if [[ "$OSTYPE" == darwin* ]]; then runtime="osx-x64"; else runtime="linux-x64"; fi
fi

if [ "$skiptests" != "true" ]; then
    echo "==> Testing"
    dotnet test "$rootdir/test/OneIdentity.Scalus.Test.csproj" --configuration "$configuration"
fi

echo "==> Publishing"
"$rootdir/scripts/publish.sh" --runtime "$runtime" --configuration "$configuration" --version "$version"

publishdir="$rootdir/Publish/$configuration/$runtime"
outputdir="$rootdir/Output/$configuration/$runtime"
mkdir -p "$outputdir"

echo "==> Packaging $runtime"
case "$runtime" in
    osx-*)
        # package.sh requires absolute paths (its resetEntitlements step cd's away).
        bash "$rootdir/scripts/Osx/package.sh" \
            --version "$version" \
            --runtime "$runtime" \
            --infile "$rootdir/scripts/Osx/applet" \
            --outpath "$outputdir" \
            --publishdir "$publishdir" \
            --isrelease "$isrelease"
        ;;
    linux-*)
        bash "$rootdir/scripts/Linux/package.sh" \
            --runtime "$runtime" --configuration "$configuration" --version "$version"
        ;;
    *)
        echo "Error: build.sh packages osx-*/linux-* only. For Windows use build.ps1." >&2
        exit 1
        ;;
esac

echo "==> Done. Artifacts in $outputdir"
ls -la "$outputdir"
