#!/bin/sh
# SCALUS post-install (deb/rpm). Refresh the desktop + icon caches so the
# menu launcher and its icon appear without a re-login. All steps are
# best-effort: a missing cache tool must never fail the package install.
set -e

if command -v update-desktop-database >/dev/null 2>&1; then
    update-desktop-database /usr/share/applications >/dev/null 2>&1 || true
fi

if command -v gtk-update-icon-cache >/dev/null 2>&1; then
    gtk-update-icon-cache --quiet /usr/share/icons/hicolor >/dev/null 2>&1 || true
fi

exit 0
