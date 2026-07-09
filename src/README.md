# SCALUS Developer Guide

SCALUS consists of 3 major components:

* The CLI: Dispatches URL's to applications. Hosts the GUI.
* The GUI: Modifies the SCALUS URL -> application configuration.
* Build & Packaging: Cross platform build and packaging.

## Working with the SCALUS CLI

### Requirements

* [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

### Build

* Using Visual Studio

    Open the scalus.sln in Visual Studio and build.

* Using dotnet CLI

    Make sure you have nuget.org configured as a package source:

    ```
    dotnet nuget list source
    ```

    If not, add it:
    
    ```
    dotnet nuget add source -n Nuget.Org https://api.nuget.org/v3/index.json
    ```

    Use the dotnet CLI to build SCALUS:

    ```
    dotnet build
    ```

### Packaging

Cross-platform build and packaging is done with plain scripts (no Cake). Each
entry point tests, publishes the NativeAOT `scalus` launcher plus the Photino
`scalus-ui` GUI, and produces an installer for the target runtime:

* macOS / Linux:

    ```
    ./build.sh --runtime osx-x64      # -> .pkg + .tar.gz
    ./build.sh --runtime linux-x64    # -> .tar.gz
    ```

* Windows (requires the `wix` dotnet tool + `WixToolset.UI.wixext`):

    ```
    ./build.ps1 -Runtime win-x64      # -> .msi
    ```

The underlying steps can also be run directly: `scripts/publish.{sh,ps1}` to
publish the payload, then the per-OS packager under `scripts/{Osx,Linux,Win}`.

### Configuration

SCALUS stores its working configuration and preferences in a per-user `SCALUS.json` file in the
user's profile folder (the exact location is OS-specific). Use the `scalus info` command to see the
full paths to the configuration and log files.

Logging is on by default at the `Debug` level and logs are written to a per-user, writable log
directory (so logging works even when SCALUS is installed under a read-only location such as
Program Files). The log level and console output are controlled from the **Settings** area of the
configuration UI, which persists them in the `settings` block of `SCALUS.json`:

```
{
  "settings": {
    "logLevel": "Debug",
    "console": false
  }
}
```

During development you can optionally drop an `appsettings.json` next to the binary to override these
(`Logging:MinLevel`, `Logging:Console`, `Logging:FileName`, `Configuration:FileName`). This file is a
development-only convenience and is not shipped with released builds.

### Usage

SCALUS command-line usage:

```
Session Client Application Launch Uri System (SCALUS)
Copyright (c) 2022 One Identity LLC

  info          Show information about the current SCALUS configuration
  launch        Launch an app configured for the specified URL
  register      Register SCALUS to handle URLs
  ui            (Default Verb) Run the configuration UI
  unregister    Unregister SCALUS for URL handling
  verify        Run a syntax check on a SCALUS configuration file
  help          Display more information on a specific command.
  version       Display version information.
```

## Working with the SCALUS GUI

See the [UI developer guide](Ui/README.md).
