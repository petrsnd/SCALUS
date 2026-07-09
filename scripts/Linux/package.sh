#!/usr/bin/env bash
#
# Assemble the Linux release tarball from a published payload.
# Mirrors the old Cake "LinuxInstall" task.
#
# Produces Output/<config>/<rid>/scalus-<version>-<rid>.tar.gz containing the
# CLI, the Photino UI payload (ui/), examples/ and the Linux setup.sh helper.
#
# Usage: scripts/Linux/package.sh --runtime linux-x64 [--configuration Release] [--version 1.0.0]
set -euo pipefail

configuration="Release"
version="1.0.0"
runtime=""

while (( "$#" )); do
    case "$1" in
        --runtime)        runtime="$2";        shift 2 ;;
        --configuration)  configuration="$2";  shift 2 ;;
        --version)        version="$2";        shift 2 ;;
        *) echo "Unknown argument: $1" >&2; exit 1 ;;
    esac
done

if [ -z "$runtime" ]; then
    echo "Error: --runtime is required (e.g. linux-x64, linux-arm64)" >&2
    exit 1
fi

scriptdir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
rootdir="$(cd "$scriptdir/../.." && pwd)"
cd "$rootdir"

publishdir="$rootdir/Publish/$configuration/$runtime"
outputdir="$rootdir/Output/$configuration/$runtime"

if [ ! -f "$publishdir/scalus" ]; then
    echo "Error: $publishdir/scalus not found. Run scripts/publish.sh first." >&2
    exit 1
fi

echo "==> Packaging Linux tarball for $runtime"
# Ship the Linux registration helper alongside the payload.
cp scripts/Linux/setup.sh "$publishdir/setup.sh"
chmod +x "$publishdir/setup.sh"

mkdir -p "$outputdir"
tarball="$outputdir/scalus-$version-$runtime.tar.gz"
rm -f "$tarball"

# Tar the contents of publishdir (not the parent path) for a clean extract.
tar -czf "$tarball" -C "$publishdir" .

echo "==> Built $tarball"
ls -la "$tarball"
