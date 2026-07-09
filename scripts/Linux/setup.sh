#!/bin/sh
#
# Install helper for the SCALUS Linux tarball. Run from the unzipped directory
# (usually via sudo) to normalise permissions and symlink the CLI onto PATH.

here=`pwd`
if [ ! -f ${here}/scalus ]; then
     here=`dirname $0`
     if  [ ! -f ${here}/scalus ]; then
        echo "Please run this script from the unzipped scalus directory"
         exit 1
     fi
fi
chown -R root:root $here
chmod 0755 `find $here -type d`
chmod 0644 `find $here -type f`
# Restore execute bits on the launcher CLI and the Photino GUI apphost (the
# blanket 0644 above clears them, which would otherwise break the GUI).
chmod 0755 $here/scalus
if [ -f ${here}/ui/scalus-ui ]; then
    chmod 0755 ${here}/ui/scalus-ui
fi

if [ -h /usr/local/bin/scalus ]; then
    rm -f /usr/local/bin/scalus
fi
ln -s ${here}/scalus /usr/local/bin/scalus

if [ -f ${here}/examples/readme.txt ]; then
    cat ${here}/examples/readme.txt
fi

