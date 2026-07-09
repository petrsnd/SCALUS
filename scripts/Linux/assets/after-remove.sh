#!/bin/sh
# SCALUS post-remove (deb/rpm). Refresh the desktop + icon caches after the
# menu launcher / icon files are gone. Best-effort; never fail the removal.
#
# Note: per-user protocol registrations (xdg-mime associations in
# ~/.local/share/applications and ~/.config) are intentionally left alone —
# they belong to individual users and are managed with `scalus unregister`.
set -e

if command -v update-desktop-database >/dev/null 2>&1; then
    update-desktop-database /usr/share/applications >/dev/null 2>&1 || true
fi

if command -v gtk-update-icon-cache >/dev/null 2>&1; then
    gtk-update-icon-cache --quiet /usr/share/icons/hicolor >/dev/null 2>&1 || true
fi

exit 0
