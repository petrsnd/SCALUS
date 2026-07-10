#!/bin/bash

version=""
runtime="osx-x64"

infile=""
outpath=""

appname="scalus"
publishdir=""
isrelease=""
scriptdir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

PARAMS=""
while(( "$#" )); do
    case "$1" in
       --version)
         if [ -z "$2" ] || [ ${2:0:1} = "-" ]; then 
            echo "Error : missing version"
            shift
            exit 1
     fi
         version="$2"
     shift 2
    ;;
       --runtime)
         if [ -z "$2" ] || [ ${2:0:1} = "-" ]; then 
            echo "Error : missing runtime"
            shift
            exit 1
     fi
         runtime="$2"
     shift 2
    ;;
       --infile)
         if [ -z "$2" ] || [ ${2:0:1} = "-" ]; then 
            echo "Error : missing infile"
            shift
            exit 1
     fi
         infile="$2"
     shift 2
    ;;
       --outpath)
         if [ -z "$2" ] || [ ${2:0:1} = "-" ]; then 
            echo "Error : missing outpath"
            shift
            exit 1
     fi
         outpath="$2"
     shift 2
    ;;
       --publishdir)
         if [ -z "$2" ] || [ ${2:0:1} = "-" ]; then 
            echo "Error : missing publishdir"
            shift
            exit 1
     fi
         publishdir="$2"
     shift 2
    ;;
       --isrelease)
         if [ -z "$2" ] || [ ${2:0:1} = "-" ]; then 
            echo "Error : missing isrelease"
            shift
            exit 1
     fi
         isrelease="$2"
     shift 2
    ;;
      *)
        shift
        ;;
    esac
done
  
if [ -z "${version}" ]; then
    rootdir="$(cd "$scriptdir/../.." && pwd)"
    version="$(sed -n 's/.*<VersionPrefix>\([^<]*\)<\/VersionPrefix>.*/\1/p' "$rootdir/Directory.Build.props" | head -n1 | tr -d '[:space:]')"
    if [ -z "${version}" ]; then
        echo "Error: could not read <VersionPrefix> from Directory.Build.props"
        exit 1
    fi
fi

appid="com.oneidentity.${appname}.macos"
pkgname="${appname}-${version}-${runtime}.pkg"
pkgfile="${outpath}/${pkgname}"

pkgtar="${appname}-${version}-${runtime}.tar.gz"
pkgtarfile="${outpath}/${pkgtar}"


if [ -z "${infile}" ]; then 
    echo "Missing infile"
    exit 1
fi
if [ -z "${outpath}" ]; then 
    echo "Missing outpath"
    exit 1
fi
if [ ! -f "${infile}" ]; then 
    echo "infile must contain full path of applet"
    exit 1
fi
if [ -z "${publishdir}" ]; then 
    echo "missing publishdir"
    exit 1
fi
if [ ! -f "${publishdir}/scalus" ]; then 
    echo "publishdir must be full path containing published scalus"
    exit 1
fi
if [ ! -x "${publishdir}/ui/scalus-ui" ]; then
    echo "publishdir must contain the published Photino GUI at ui/scalus-ui"
    exit 1
fi

# Normalize to absolute paths. resetEntitlements() cd's into the source app
# template and never returns, so any later use of a *relative* outpath /
# publishdir / infile would resolve against the wrong directory and fail
# (e.g. "missing entitlements.plist"). CI already passes absolute paths; this
# makes local invocation with relative paths work too.
mkdir -p "${outpath}"
outpath="$(cd "${outpath}" && pwd)"
publishdir="$(cd "${publishdir}" && pwd)"
infile="$(cd "$(dirname "${infile}")" && pwd)/$(basename "${infile}")"
pkgfile="${outpath}/${pkgname}"
pkgtarfile="${outpath}/${pkgtar}"


echo "Building from ${infile}"
echo "Building ${pkgfile}"


tmpdir="${outpath}/tmp"

function resetInfo()
{

    filename="${tmpdir}/${appname}.app/Contents/Info.plist"
    if [ ! -f ${filename} ]; then 
    echo "ERROR - missing file:${filename}"
        exit 1
    fi
    /bin/bash -c "defaults write $filename CFBundleURLTypes  -array \
'
<array>
    <dict>
        <key>CFBundleTypeRole</key>
        <string>Viewer</string>
        <key>CFBundleURLName</key>
        <string>scalus telnet URL</string>
        <key>CFBundleURLSchemes</key>
        <array>
            <string>telnet</string>
        </array>
    </dict>
    <dict>
        <key>CFBundleTypeRole</key>
        <string>Viewer</string>
        <key>CFBundleURLName</key>
        <string>scalus ssh URL</string>
        <key>CFBundleURLSchemes</key>
        <array>
            <string>ssh</string>
        </array>
    </dict>
    <dict>
        <key>CFBundleTypeRole</key>
        <string>Viewer</string>
        <key>CFBundleURLName</key>
        <string>scalus rdp URL</string>
        <key>CFBundleURLSchemes</key>
        <array>
            <string>rdp</string>
        </array>
    </dict>
</array> '"


/bin/bash -c "defaults write $filename CFBundleVersion  -string \"${version}\""
/bin/bash -c "defaults write $filename CFBundleShortVersion  -string \"${version}\""
/bin/bash -c "defaults write $filename CFBundleName  -string 'Scalus'"
/bin/bash -c "defaults write $filename CFBundleDisplayName  -string 'Session URL Launcher Utility'"
/bin/bash -c "defaults write $filename CFBundleIdentifier  -string  '${appid}'"
/bin/bash -c "defaults write $filename ITSAppUsesNonExemptEncryption -bool false"
/bin/bash -c "defaults write $filename LSApplicationCategoryType -string 'public.app-category.developer-tools'"
/bin/bash -c "defaults write $filename LSMinimumSystemVersion -string '10.6.0'"

    chmod a+r $filename
}

function resetEntitlements()
{
    cd ${scriptdir}/${appname}.app/Contents
    ls
    cp entitlements.plist ${tmpdir}/${appname}.app/Contents/entitlements.plist   
    if [ ! -f ${tmpdir}/${appname}.app/Contents/entitlements.plist ]; then 
        echo "ERROR - missing file: ${tmpdir}/${appname}.app/Contents/entitlements.plist"
        exit 1
    fi 
    chmod a+r ${tmpdir}/${appname}.app/Contents/entitlements.plist
}

function make_app()
{
    if [ -d ${tmpdir} ]; then 
            rm -rf ${tmpdir}
    fi
        mkdir -p ${tmpdir}

        cat ${infile} | awk -v appname="${appid}" '
{
        str=sprintf("kMDItemCFBundleIdentifier=%s", appname);
        sub(/kMDItemCFBundleIdentifier=\S+/, str);
    print $0
}' > ${infile}.tmp
if [ $? -eq 0 ]; then 
    mv ${infile}.tmp ${infile}
fi

    osacompile -o ${tmpdir}/${appname}.app ${infile}
    resetInfo
    resetEntitlements

    cp $publishdir/scalus ${tmpdir}/${appname}.app/Contents/MacOS
    chmod u=rwx,go=rx  ${tmpdir}/${appname}.app/Contents/MacOS/scalus

    # Bundle the Photino config GUI payload (scalus-ui + native webview dylib +
    # wwwroot + runtime files) under Contents/MacOS/ui. The applet launches
    # ui/scalus-ui on normal open; URL launches route to the scalus CLI above.
    mkdir -p ${tmpdir}/${appname}.app/Contents/MacOS/ui
    cp -R $publishdir/ui/ ${tmpdir}/${appname}.app/Contents/MacOS/ui
    chmod -R a+rX ${tmpdir}/${appname}.app/Contents/MacOS/ui
    chmod u=rwx,go=rx ${tmpdir}/${appname}.app/Contents/MacOS/ui/scalus-ui

    mkdir -p ${tmpdir}/${appname}.app/Contents/Resources/examples
    chmod a+rx ${tmpdir}/${appname}.app/Contents/Resources/Examples

    cp $publishdir/examples/*  ${tmpdir}/${appname}.app/Contents/Resources/examples
    chmod a+r ${tmpdir}/${appname}.app/Contents/Resources/examples/*

    # Replace the stock osacompile applet icon (a generic scroll) with the SCALUS
    # logomark. osacompile sets CFBundleIconFile=applet, so overwriting
    # Resources/applet.icns is all that's needed — no Info.plist change.
    iconsrc="${scriptdir}/assets/scalus.icns"
    if [ -f "${iconsrc}" ]; then
        cp "${iconsrc}" ${tmpdir}/${appname}.app/Contents/Resources/applet.icns
        chmod a+r ${tmpdir}/${appname}.app/Contents/Resources/applet.icns
    else
        echo "[WARN] ${iconsrc} not found; app will use the default applet icon"
    fi

    if [ "$isrelease" = "False" ]; then
        echo "[INFO] Not signing the app bundle files as this is not a release build"
    else
        # CodeSigning the files in the app bundle
        echo "[INFO] Signing the app bundle files"
        codesign --force --entitlements "${tmpdir}/${appname}.app/Contents/entitlements.plist" -s LDBTVAT43D -v "${tmpdir}/${appname}.app" --deep --strict --options=runtime --timestamp
        codesign -vvv --deep --strict "${tmpdir}/${appname}.app" 
        echo "[INFO] Removing extended attributes from the app bundle"
        xattr -c "${tmpdir}/${appname}.app"
        if [ $? -ne 0 ]; then
            echo "*** Failed to sign the app bundle"
            exit 1
        fi
    fi

    here=`pwd`
    cd $tmpdir
    tar -cvf - ${appname}.app | gzip -c > ${pkgtarfile}
    cd $here
}


function build_package()
{
    rm -f ${pkgfile}
    productbuild --component  ${tmpdir}/${appname}.app "Applications" ${pkgfile}
    expdir="${outpath}/tmp2"
    rm -rf ${expdir}

    pkgutil --expand $pkgfile ${expdir}
    cat ${expdir}/Distribution | awk -v pkg="${pkgname}" -v appname="${appname}" '
{
    str=""
    sub(/customLocation=\"[^\"]+\"/, str);
    if ($0 ~ /<\/installer-gui-script>/)
    {
        print "  <domains enable_anywhere=\"false\" enable_currentUserHome=\"true\" enable-localSystem=\"true\">"
        print "  </domains>"
    }

    print $0;
}' > ${expdir}/Distribution2
    if [ $? -ne 0 ]; then
        echo "*** Failed to change file"
        exit 1
    fi
    mv ${expdir}/Distribution2 ${expdir}/Distribution
    
    pkgutil --flatten ${expdir} ${pkgfile}
        rm -rf ${tmpdir}
        rm -rf ${expdir}
}
make_app
build_package

echo "Finished generating ${pkgfile}"
