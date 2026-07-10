# Upgrading to SCALUS 2.0

SCALUS 2.0 is a **major release**. The technology stack, user interface, and
several storage locations changed, so this is not a drop-in patch over a 1.x
install. This document explains what changes, what carries over automatically,
and what you need to do by hand.

If you are installing SCALUS for the first time, you can ignore this file — just
run the installer for your platform.

---

## What changed in 2.0 (the short version)

- **New configuration UI.** The old local web server + system-browser settings
  page is gone. Configuration now runs in a native desktop window (`scalus-ui`)
  built on Angular + Photino. No browser, no `localhost` port.
- **Two binaries instead of one.** A tiny native launcher, `scalus`, handles the
  hot path (every `rdp://` / `ssh://` click and protocol registration). A separate
  `scalus-ui` app handles configuration. Both share one core library.
- **Per-user configuration.** Config and logs now live in per-user, writable
  locations instead of next to the binary, so editing settings never needs
  elevation.
- **Modern runtime.** Rebuilt on .NET 10 with a NativeAOT launcher (fast cold
  start, self-contained, no runtime install required).
- **Single, tag-driven version.** All version numbers come from one checked-in
  source and, for releases, from a git tag (see "Versioning & releases" below).

---

## Platform-by-platform upgrade behavior

### Windows

- The 2.0 MSI **upgrades an existing 1.x install in place.** It keeps the same
  Windows `UpgradeCode` and uses a `MajorUpgrade`, so installing 2.0 removes the
  old version and installs the new one automatically — no manual uninstall needed.
- **Protocol registrations:** re-register your protocols from the new UI (or run
  `scalus register`) after upgrading. Handlers now point at the canonical
  `scalus.exe launch` command; opening the config UI and toggling a protocol
  rewrites any stale 1.x handler to the new form.
- **Configuration does not move automatically.** In 1.x, `scalus.json` lived next
  to the binary (under `Program Files`). In 2.0 it lives per-user at
  `%LOCALAPPDATA%\SCALUS\scalus.json`. On first launch the new install seeds a
  fresh, platform-appropriate default config there. To bring your old settings
  forward, use **Import** in the new UI (see "Migrating your old configuration").

### macOS

- macOS never shipped in 1.x, so 2.0 is a **fresh install** — there is nothing to
  upgrade. Install the `.pkg`, launch the app once, and register your protocols.

### Linux

- Linux never shipped a supported 1.x package, so 2.0 is effectively a **fresh
  install** (`.deb`, `.rpm`, or the portable `.tar.gz`).
- If you ran an unofficial 1.x build that stored config under `~/.SCALUS`, the
  first 2.0 launch **copies that config forward once** to the XDG location
  (`~/.config/scalus/`, honoring `$XDG_CONFIG_HOME`). Logs and per-launch records
  from the old directory are intentionally not copied. This is best-effort and
  never blocks startup.

---

## Migrating your old configuration

There is **no automatic import of a 1.x Windows `scalus.json`** into 2.0 (the file
location and, in places, the schema changed). Instead, 2.0 gives you an explicit,
validated path:

1. Keep a copy of your old `scalus.json` before/while upgrading (on Windows it was
   in the SCALUS install directory).
2. Open the new `scalus-ui` configuration app.
3. Go to **Import / Export** and import your old config. Import validates against
   the current schema and does a best-effort mapping rather than a blind load, so
   you will be told if something can't be carried over.
4. You can also import a **single application** definition (per-app share files)
   from the Applications screen if you only want to move specific clients.

Older template forms (e.g. default/inline RDP templates) are migrated on disk
automatically the first time 2.0 loads an existing config, so imported configs are
brought up to the current on-disk shape.

> Note: SCALUS never stores credentials. Configuration is only launch templates
> (which client, which arguments, which generated file), so importing an old
> config carries no secrets.

---

## Versioning & releases (new model)

Starting with 2.0, SCALUS follows **trunk-based development with tag-driven
releases**, the same model used by the OneIdentity Safeguard SDKs.

- There is **one checked-in version source**: `Directory.Build.props`
  (`<VersionPrefix>`). Nothing hardcodes a version anywhere else — build,
  publish, and packaging scripts all read it.
- **Local / trunk / PR builds** produce `X.Y.Z.<build>` where `<build>` is an
  incrementing CI build number (0 for a plain local build). These are marked
  prerelease.
- **Releases** are cut by pushing a git tag `vX.Y.Z`. CI verifies the tag matches
  `<VersionPrefix>` exactly (a mismatch fails the build on purpose), then produces
  a clean `X.Y.Z` release and drafts a GitHub release with the installers attached.

To cut the next release:

1. Land the desired `<VersionPrefix>` on `master` (e.g. `2.0.0`).
2. Tag it: `git tag v2.0.0 && git push origin v2.0.0`.
3. CI builds, signs, and drafts the GitHub release with the MSI / PKG / DEB / RPM
   / tarball artifacts.

Installer artifacts always use a purely numeric version (installers reject
`-pre`-style suffixes); prerelease status is conveyed by the GitHub release flag,
not by the version string.
