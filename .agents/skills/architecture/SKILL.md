---
name: architecture
description: Use when reasoning about SCALUS component boundaries, the URI dispatch/launch flow, protocol registration, or the two-binary/shared-core model.
---

# SCALUS Architecture

Read this when you need to understand how a clicked URI becomes a launched
session client, where a given responsibility lives, or why SCALUS is split the
way it is.

## The one-sentence model

The OS is configured to invoke **`scalus launch -u <uri>`** whenever a registered
protocol URI is opened; SCALUS parses the URI, matches it to a configured
application + template, materializes any needed files, and starts the session
client.

## Two binaries, one core

| Binary | Project | Role |
|--------|---------|------|
| `scalus` | `src/Cli/Scalus.Cli.csproj` | NativeAOT launcher + CLI. The OS hot path (`launch`) and the `register`/`unregister`/`info`/`verify` verbs. |
| `scalus-ui` | `src/Scalus.Ui/Scalus.Ui.csproj` | Photino desktop app for editing configuration. |
| *(shared)* | `src/OneIdentity.Scalus.csproj` → `Scalus.Core.dll` | Everything real: parsing, config, templates, platform services, registration, dispatch. |

Both apphosts funnel through **`CommandLineRunner.Run(args)`** so the CLI and the
UI executable (when the OS invokes it as a handler) behave identically. **Put
logic in the core**, not in an apphost. `Ioc.cs` is the DI composition root that
wires platform-specific implementations.

Why two binaries: the launch hot path must be tiny, fast, and dependency-free
(NativeAOT `scalus`), while the config UI carries a whole Angular/Photino/GTK
stack that must never sit on the hot path.

## CLI verbs

Verb implementations live in sibling folders, each with an `Options.cs`:
`Launch/`, `Register/`, `Unregister/`, `Info/`, `Verify/`. These five verbs are
registered in `Ioc.cs` (`RegisterVerbs`); `IVerb.cs` / `CommandLineHandler.cs`
define dispatch. The configuration GUI is **not** a verb — it is the separate
`scalus-ui` binary, which opens the UI when run with no launch arguments.

- `launch  -u <uri>` — the hot path (see flow below). Only this verb is ever
  invoked by the OS.
- `register` / `unregister` — write/remove OS handler associations.
- `info` — print resolved config/log paths and current registration state.
- `verify` — syntax-check a `SCALUS.json`.

## The launch (dispatch) flow

1. **Parse** — `UrlParser/` turns the URI into tokens. `ProtocolHandlerFactory`
   selects a parser by protocol (`rdp`, `ssh`, `telnet`, and a default parser);
   parsers expose URL parts as substitution tokens.
2. **Resolve** — the protocol → application mapping in `SCALUS.json` picks the
   configured application for that protocol on this platform.
3. **Template** — the application's template is rendered with the parsed tokens,
   producing command-line args and/or a generated file (e.g. an `.rdp` file).
4. **Terminal wrap (optional)** — for console clients, the preferred-terminal
   resolver wraps the command in the chosen terminal.
5. **Launch** — the platform `OsServices` implementation starts the client and
   writes a per-launch record for the UI's recent-launches view.

## Protocol registration

Registration is a per-user runtime action (`scalus register`), not something the
installer does. Platform services under `src/Platform/{Windows,MacOS,Linux}`
implement it:

- **Windows** — registry URI-scheme handler entries.
- **macOS** — Launch Services associations for the app's declared schemes.
- **Linux** — a generated `scalus.desktop` + `xdg-mime` associations.

All-users (machine-wide) registration on desktop platforms needs elevation; an
elevation broker shells out to a sibling `scalus` binary. Keep that in mind when
touching registration or packaging (the sibling must exist next to the UI).

## Configuration & templates

`SCALUS.json` is per-user and writable (so registration/edits never need
elevation). It holds `applications`, protocol→application `mappings`, and a
`settings` block. Config lives at an OS-specific path (Windows `%LOCALAPPDATA%`,
macOS `~/Library/Application Support/SCALUS`, Linux `~/.config/scalus`). Schema,
seeding, platform filtering, and legacy migration are in the **configuration**
skill.

## The UI bridge (very short)

`scalus-ui` is an Angular app in a Photino window. It calls C# through a
**bridge** (not HTTP): `BridgeDispatcher.cs` on the C# side, `ScalusBridge` on the
TS side. Full end-to-end recipe is in the **ui-bridge** skill.

## Where to make a change

| Task | Start in |
|------|----------|
| New/changed URI parsing | `src/UrlParser/` + `ProtocolHandlerFactory` |
| New CLI verb / verb behavior | `src/<Verb>/` + `CommandLineHandler.cs` |
| OS registration behavior | `src/Platform/{Windows,MacOS,Linux}` |
| Launch/template rendering | `src/Launch/`, template + `UrlParser` token code |
| Config shape / migration | `src/ScalusConfigurationBase.cs`, `Util/ConfigurationManager.cs`, `Dto/` |
| UI capability | `ui/` + `BridgeDispatcher.cs` (see ui-bridge skill) |
| Build/version/packaging | `scripts/`, `Directory.Build.props` (see build-and-release skill) |
