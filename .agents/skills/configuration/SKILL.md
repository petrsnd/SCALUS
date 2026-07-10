---
name: configuration
description: Use when working on SCALUS.json — its schema (applications/protocols/settings), template rendering, default seeding, platform filtering, or legacy migration and import/export.
---

# SCALUS Configuration

Read this when touching `SCALUS.json`, the config DTOs, template rendering, seeding
of a fresh config, platform filtering, or migration/import.

## Where config lives (per-user, writable)

`SCALUS.json` is per-user so edits and registration never need elevation:

| OS | Path |
|----|------|
| Windows | `%LOCALAPPDATA%\SCALUS\SCALUS.json` |
| macOS | `~/Library/Application Support/SCALUS/SCALUS.json` |
| Linux | `$XDG_CONFIG_HOME/scalus/SCALUS.json` (else `~/.config/scalus/SCALUS.json`) |

`Util/ConfigurationManager.cs` resolves these (`ProdAppPath`). Logs live in a
separate per-user dir (`LogDir`): `%LOCALAPPDATA%\SCALUS\logs`, `~/Library/Logs/
SCALUS`, or on Linux `$XDG_STATE_HOME/scalus/logs` (else `~/.local/state/scalus/logs`).

## Schema (the DTOs in `src/Dto/`)

```jsonc
{
  "Applications": [            // reusable client launch definitions
    {
      "Id": "win-rdp",
      "Name": "Remote Desktop",
      "Platforms": ["Windows"],// which OSes this app is valid on
      "Protocol": "rdp",
      "Exec": "mstsc.exe",
      "Args": ["%GeneratedFile%"],
      "Parser": {              // how the URI is parsed + templated
        "ParserId": "rdp",     // rdp | ssh | telnet | url (default)
        "TemplateContent": "...","TemplateExtension": ".rdp",
        "LineEnding": "CrLf", "Encoding": "Utf16LeBom",
        "RunInTerminal": false,
        "PostProcessingExec": null, "PostProcessingArgs": []
      }
    }
  ],
  "Protocols": [               // scheme -> application pointers
    { "Protocol": "rdp", "AppId": "win-rdp" },
    { "Protocol": "telnet", "AppId": null }   // legal: a scheme may have no app
  ],
  "PreferredTerminal": null,   // global terminal for RunInTerminal launches; null/auto = detect
  "Settings": { "LogLevel": "Debug", "Console": false }  // user prefs read by the launcher
}
```

Key rules:
- **`Protocol` is required; `AppId` is optional.** A protocol with no app is valid
  and serializable — it just can't be registered/launched until an app is assigned.
  `ScalusConfig.Validate()` skips app-less mappings and only errors on an `AppId`
  that points to a missing or protocol-mismatched app.
- **`Applications` are the only createable/shareable unit.** Protocol rows are just
  pointers (scheme → app + registration state).
- **`Settings` and `PreferredTerminal` are top-level.** `GetConfiguration()` **must
  preserve every top-level field** on read, or the UI silently wipes them on the
  next save. This was a real bug — don't reintroduce it.
- JSON round-trips through `Util/ScalusJson.cs` (System.Text.Json **source-gen**
  context — NativeAOT-safe). Reads are case-insensitive, so legacy PascalCase/
  camelCase both load. Any new DTO must be reachable from `ScalusConfig` so the
  source-gen context covers it.

## Templates & tokens

A `Parser` turns the URI into tokens and renders `Args` and/or a generated file
(e.g. `.rdp`). `ParserId` selects the parser (`rdp`/`ssh`/`telnet`/`url`);
`ProtocolHandlerFactory.GetSupportedParsers()` is the live list. `%Token%`
placeholders (e.g. `%Host%`, `%User%`, `%Port%`, `%GeneratedFile%`) are substituted
during launch. `LineEnding`/`Encoding` control the generated file's exact bytes
(RDP needs `CrLf` + `Utf16LeBom`). `PostProcessingExec` runs a step on the
generated file before launch (e.g. `rdpsign.exe`).

## Default seeding + platform filtering

The master seed is **`src/SCALUS.json`** — it lists **every** application across
all platforms, each tagged with its valid `Platforms`. On first run, seeding
filters that master list down to the running OS via `Util/PlatformFilter.cs`, so a
fresh Windows config never offers a macOS client. This replaced the old
hand-synced per-platform seeds (`scripts/{Win,Osx,Linux}/SCALUS.json`, now gone).
When adding a shipped app, add it once to `src/SCALUS.json` with correct
`Platforms`.

## Legacy migration & import

SCALUS 2.0 is a clean-break upgrade (fresh install, new config location/schema),
but old configs are still importable/migratable:

- **`ScalusConfigurationBase.MigrateLegacyTemplates(config)`** (runs inside
  `Load(path)`) folds old `UseDefaultTemplate` / `UseTemplateFile` parser forms into
  inline `TemplateContent`. So loading a 1.x config just works.
- **`ConfigurationManager.MigrateLegacyLinuxConfig`** one-time copies an old
  `~/.SCALUS` config forward to the XDG dir (SCALUS.json + template/support files,
  excluding logs), so Linux users aren't orphaned.
- **Import (UI)** — `importFromFile` (`BridgeDispatcher.cs`) opens a file dialog and
  returns the file's text; the Angular app parses and normalizes it (`app.ts`,
  `seed-data.ts`) before applying. The Import/Export screen offers whole-config
  **replace** and per-application **merge** (with name-collision handling), and
  `exportToFile` writes a config or a single-application fragment. Note the UI
  import path normalizes JSON in TypeScript — it does **not** itself call
  `MigrateLegacyTemplates`; the on-disk template migration above runs when the core
  loads a config. `test/TestMigrationFixtures.cs` (+ the `legacy-config.json`
  fixture) covers that core migration load path, not the UI import wiring.

## Verifying changes

```powershell
dotnet test test/OneIdentity.Scalus.Test.csproj    # migration + round-trip + validation specs
dotnet run --project src/Cli/Scalus.Cli.csproj -- verify -p <path-to-SCALUS.json>
```

`test/TestConfigRoundTrip.cs` guards that a save/reload preserves fields exactly and
adds nothing spurious. Fixtures pin line endings via `.gitattributes` so CRLF/LF
assertions stay meaningful.
