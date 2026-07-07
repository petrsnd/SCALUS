// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationManager.cs" company="One Identity Inc.">
//   This software is licensed under the Apache 2.0 open source license.
//   https://github.com/OneIdentity/SCALUS/blob/master/LICENSE
//
//
//   Copyright One Identity LLC.
//   ALL RIGHTS RESERVED.
//
//   ONE IDENTITY LLC. MAKES NO REPRESENTATIONS OR
//   WARRANTIES ABOUT THE SUITABILITY OF THE SOFTWARE,
//   EITHER EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
//   TO THE IMPLIED WARRANTIES OF MERCHANTABILITY,
//   FITNESS FOR A PARTICULAR PURPOSE, OR
//   NON-INFRINGEMENT.  ONE IDENTITY LLC. SHALL NOT BE
//   LIABLE FOR ANY DAMAGES SUFFERED BY LICENSEE
//   AS A RESULT OF USING, MODIFYING OR DISTRIBUTING
//   THIS SOFTWARE OR ITS DERIVATIVES.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace OneIdentity.Scalus.Util
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using Microsoft.Extensions.Configuration;
    using OneIdentity.Scalus.Dto;
    using Serilog.Events;
    using ScalusJsonIo = OneIdentity.Scalus.Util.ScalusJson;

    public static class ConfigurationManager
    {
        public const string ProdName = "SCALUS";

        // Shared Serilog output template (both processes) so the launcher and UI logs interleave
        // cleanly when merged in the viewer. {LaunchId} is empty for non-launch lines and carries a
        // pre-formatted per-launch correlation tag once a launch pushes it onto the LogContext.
        public const string LogOutputTemplate =
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {LaunchId}{Message:lj}{NewLine}{Exception}";

        // Daily rolling with ~14 files retained ≈ two weeks of history per process.
        public const int LogRetainedFileCountLimit = 14;

        private const string LogFileSetting = "Logging:fileName";
        private const string ConfigFileSetting = "Configuration:fileName";
        private const string MinLogLevelSetting = "Logging:MinLevel";
        private const string LogToConsoleSetting = "Logging:Console";
        private const string JsonFile = ProdName + ".json";
        private const string LauncherLogBaseName = "launcher-.log";
        private const string UiLogBaseName = "ui-.log";
        private const string Examples = "examples";

        private static string examplePath;
        private static string prodAppPath;
        private static string logDir;
        private static string launcherLogFile;
        private static string uiLogFile;
        private static string scalusJson;
        private static string scalusJsonDefault;

        private static bool settingsLoaded;
        private static ScalusSettings cachedSettings;

        private static IConfiguration appSetting;

        static ConfigurationManager()
        {
            string path = string.Empty;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                path = ProdAppPath;
            }
            else
            {
                path = Constants.GetBinaryDirectory();
            }

            var fname = Path.Combine(path, "appsettings.json");

            if (File.Exists(fname))
            {
                appSetting = new ConfigurationBuilder()
                    .SetBasePath(path)
                    .AddJsonFile("appsettings.json", true)
                    .Build();
            }
        }

        public static string ExamplePath
        {
            get
            {
                if (!string.IsNullOrEmpty(examplePath))
                {
                    return examplePath;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    examplePath = Path.Combine(Path.Combine(Path.GetDirectoryName(Constants.GetBinaryDirectory()), "Resources"), Examples);
                }
                else
                {
                    examplePath = Path.Combine(Constants.GetBinaryDirectory(), Examples);
                }

                if (Directory.Exists(examplePath))
                {
                    return examplePath;
                }

                examplePath = string.Empty;
                return examplePath;
            }
        }

        public static string ProdAppPath
        {
            get
            {
                if (!string.IsNullOrEmpty(prodAppPath))
                {
                    return prodAppPath;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    prodAppPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create), ProdName);
                    if (!Directory.Exists(prodAppPath))
                    {
                        Directory.CreateDirectory(prodAppPath);
                    }

                    return prodAppPath;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    var path =
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.Create), "Library");

                    prodAppPath = $"{path}/Application Support/{ProdName}";
                    if (!Directory.Exists(prodAppPath))
                    {
                        Directory.CreateDirectory(prodAppPath);
                    }

                    return prodAppPath;
                }

                prodAppPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.Create),
                    $".{ProdName}");
                if (!Directory.Exists(prodAppPath))
                {
                    Directory.CreateDirectory(prodAppPath);
                }

                return prodAppPath;
            }
        }

        // Per-user, writable directory for logs (and log-adjacent state). Kept SEPARATE from the
        // config dir (ProdAppPath) so Linux can follow the XDG split (config in ~/.config, logs/state
        // in ~/.local/state) while Windows/macOS use their platform log conventions. This is always a
        // user-writable location, so logging works even when the binaries are installed under a
        // read-only path such as Program Files.
        public static string LogDir
        {
            get
            {
                if (!string.IsNullOrEmpty(logDir))
                {
                    return logDir;
                }

                logDir = EnsureWritableDir(ComputeLogDir());
                return logDir;
            }
        }

        // Base path for the CLI launcher's rolling log (Serilog appends the date, e.g.
        // launcher-20260707.log). A dev-only appsettings.json may override the file name.
        public static string LauncherLogFile
        {
            get
            {
                if (!string.IsNullOrEmpty(launcherLogFile))
                {
                    return launcherLogFile;
                }

                // An optional dev-only appsettings.json may override the log file name; a relative
                // name resolves against the per-user LogDir (never the read-only binary dir).
                if (!string.IsNullOrEmpty(appSetting?[LogFileSetting]))
                {
                    launcherLogFile = FullPath(appSetting[LogFileSetting], LogDir);
                    return launcherLogFile;
                }

                launcherLogFile = Path.Combine(LogDir, LauncherLogBaseName);
                return launcherLogFile;
            }
        }

        // Base path for the configuration UI's rolling log (e.g. ui-20260707.log). Kept as a separate
        // file so the two processes never contend for one handle; the viewer merges them by timestamp.
        public static string UiLogFile
        {
            get
            {
                if (!string.IsNullOrEmpty(uiLogFile))
                {
                    return uiLogFile;
                }

                uiLogFile = Path.Combine(LogDir, UiLogBaseName);
                return uiLogFile;
            }
        }

        public static string ScalusJson
        {
            get
            {
                if (!string.IsNullOrEmpty(scalusJson))
                {
                    return scalusJson;
                }

                if (!string.IsNullOrEmpty(appSetting?[ConfigFileSetting]))
                {
                    scalusJson = FullPath(appSetting[ConfigFileSetting], ProdAppPath);
                    return scalusJson;
                }

                // Persist the working configuration in the per-user application data
                // directory on every platform. This keeps the CLI launcher and the
                // configuration UI pointed at the same file, and keeps the config
                // writable even when SCALUS is installed under a read-only location
                // such as Program Files.
                scalusJson = Path.Combine(ProdAppPath, JsonFile);
                return scalusJson;
            }
        }

        public static string ScalusJsonDefault
        {
            get
            {
                if (!string.IsNullOrEmpty(scalusJsonDefault))
                {
                    return scalusJsonDefault;
                }

                scalusJsonDefault = Path.Combine(Constants.GetBinaryDirectory(), JsonFile);
                if (!File.Exists(scalusJsonDefault))
                {
                    scalusJsonDefault = Path.Combine(Path.Combine(ExamplePath, JsonFile));
                }

                if (!File.Exists(scalusJsonDefault))
                {
                    scalusJsonDefault = string.Empty;
                }

                return scalusJsonDefault;
            }
        }

        public static LogEventLevel? MinLogLevel => ParseLevel();

        public static bool LogToConsole => ParseConsoleLogging();

        private static string FullPath(string path, string baseDir)
        {
            if (Path.IsPathFullyQualified(path))
            {
                return path;
            }

            var fqpath = Path.Combine(baseDir, path);
            var dir = Path.GetDirectoryName(fqpath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            return fqpath;
        }

        // Resolve the per-user log directory per platform. Windows: %LOCALAPPDATA%\SCALUS\logs.
        // macOS: ~/Library/Logs/SCALUS (the platform log convention). Linux: XDG state dir
        // ($XDG_STATE_HOME/scalus/logs, falling back to ~/.local/state/scalus/logs) — logs are
        // "state" in the XDG spec, not data. Lowercase "scalus" on Linux; branded casing elsewhere.
        private static string ComputeLogDir()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create),
                    ProdName,
                    "logs");
            }

            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.Create);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return Path.Combine(home, "Library", "Logs", ProdName);
            }

            var state = Environment.GetEnvironmentVariable("XDG_STATE_HOME");
            if (string.IsNullOrEmpty(state) || !Path.IsPathFullyQualified(state))
            {
                state = Path.Combine(home, ".local", "state");
            }

            return Path.Combine(state, "scalus", "logs");
        }

        // Make sure the chosen directory exists and is usable; if creating it fails (unexpected
        // permission problem), degrade to the temp directory so logging never breaks a protocol
        // launch.
        private static string EnsureWritableDir(string dir)
        {
            try
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                return dir;
            }
            catch (Exception)
            {
                var fallback = Path.Combine(Path.GetTempPath(), ProdName, "logs");
                try
                {
                    Directory.CreateDirectory(fallback);
                }
                catch (Exception)
                {
                    return Path.GetTempPath();
                }

                return fallback;
            }
        }

        // The user-editable preferences persisted in SCALUS.json. Read once and cached: the level is
        // consumed during logging bootstrap (before SCALUS.json may exist on first run), so a missing
        // or malformed file must yield null rather than throw, letting the code defaults win.
        private static ScalusSettings ReadSettings()
        {
            if (settingsLoaded)
            {
                return cachedSettings;
            }

            settingsLoaded = true;
            try
            {
                var path = ScalusJson;
                if (File.Exists(path))
                {
                    cachedSettings = ScalusJsonIo.Deserialize(File.ReadAllText(path))?.Settings;
                }
            }
            catch (Exception)
            {
                cachedSettings = null;
            }

            return cachedSettings;
        }

        // Minimum log level, resolved in precedence order: (1) the user's SCALUS.json Settings block,
        // (2) an optional dev-only appsettings.json override, (3) the code default of Debug — verbose
        // detail is on by default so a failed launch is always inspectable.
        private static LogEventLevel? ParseLevel()
        {
            var settingLevel = ReadSettings()?.LogLevel;
            if (!string.IsNullOrWhiteSpace(settingLevel) &&
                Enum.TryParse<LogEventLevel>(settingLevel, true, out var userLevel))
            {
                return userLevel;
            }

            var val = appSetting?[MinLogLevelSetting];
            if (!string.IsNullOrWhiteSpace(val) &&
                Enum.TryParse<LogEventLevel>(val, true, out var appLevel))
            {
                return appLevel;
            }

            return LogEventLevel.Debug;
        }

        // Console logging, resolved in the same precedence order (Settings, then dev appsettings,
        // then the code default of off).
        private static bool ParseConsoleLogging()
        {
            var settingConsole = ReadSettings()?.Console;
            if (settingConsole.HasValue)
            {
                return settingConsole.Value;
            }

            var val = appSetting?[LogToConsoleSetting];
            if (bool.TryParse(val, out var bval))
            {
                return bval;
            }

            return false;
        }
    }
}
