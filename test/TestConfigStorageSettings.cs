using System;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;
using OneIdentity.Scalus;
using OneIdentity.Scalus.Dto;
using OneIdentity.Scalus.Util;
using Serilog.Events;

namespace OneIdentity.Scalus.Test
{
    // Locks in the step-1a "config/logging foundation" behavior:
    //  - GetConfiguration() no longer strips PreferredTerminal / Settings (the pre-existing bug that
    //    silently wiped the shipped PreferredTerminal feature on the next save).
    //  - The Settings block round-trips through the real save -> Load pipeline.
    //  - Logs resolve to a per-user, writable directory with NO appsettings.json present (the
    //    Program Files read-only problem), and the minimum level always resolves (Debug by default).
    public class TestConfigStorageSettings
    {
        private sealed class DiskLoader : ScalusConfigurationBase
        {
            public DiskLoader()
            {
            }

            public DiskLoader(ScalusConfig config)
            {
                Config = config;
            }

            public ScalusConfig LoadFrom(string path) => Load(path);
        }

        private static ScalusConfig SaveAndReload(ScalusConfig config)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "scalus-cfg-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                var path = Path.Combine(tempDir, "SCALUS.json");
                File.WriteAllText(path, ScalusJson.Serialize(config));
                return new DiskLoader().LoadFrom(path);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void GetConfigurationPreservesPreferredTerminalAndSettings()
        {
            var config = new ScalusConfig
            {
                PreferredTerminal = "wt",
                Settings = new ScalusSettings { LogLevel = "Verbose", Console = true },
            };

            var projected = new DiskLoader(config).GetConfiguration();

            Assert.Equal("wt", projected.PreferredTerminal);
            Assert.NotNull(projected.Settings);
            Assert.Equal("Verbose", projected.Settings.LogLevel);
            Assert.True(projected.Settings.Console);
        }

        [Fact]
        public void SettingsRoundTripThroughSaveAndReload()
        {
            var config = new ScalusConfig
            {
                PreferredTerminal = "iterm",
                Settings = new ScalusSettings { LogLevel = "Warning", Console = false },
            };

            var reloaded = SaveAndReload(config);

            Assert.Equal("iterm", reloaded.PreferredTerminal);
            Assert.NotNull(reloaded.Settings);
            Assert.Equal("Warning", reloaded.Settings.LogLevel);
            Assert.False(reloaded.Settings.Console);
        }

        [Fact]
        public void SettingsIsOmittedWhenNull()
        {
            var json = ScalusJson.Serialize(new ScalusConfig());

            // WhenWritingNull: an untouched config must not sprout a settings block.
            Assert.DoesNotContain("\"settings\"", json, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void LogDirIsPerUserWritableAndNotTheBinaryDirectory()
        {
            var dir = ConfigurationManager.LogDir;

            Assert.True(Directory.Exists(dir), $"LogDir should exist: {dir}");
            Assert.NotEqual(
                Path.GetFullPath(AppContext.BaseDirectory).TrimEnd(Path.DirectorySeparatorChar),
                Path.GetFullPath(dir).TrimEnd(Path.DirectorySeparatorChar));

            // Prove it is actually writable (the whole point of moving off Program Files).
            var probe = Path.Combine(dir, "probe-" + Guid.NewGuid().ToString("N") + ".tmp");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);
        }

        [Fact]
        public void LogFilesLiveInLogDir()
        {
            Assert.Equal(
                Path.GetFullPath(ConfigurationManager.LogDir),
                Path.GetFullPath(Path.GetDirectoryName(ConfigurationManager.LauncherLogFile)));
            Assert.Equal(
                Path.GetFullPath(ConfigurationManager.LogDir),
                Path.GetFullPath(Path.GetDirectoryName(ConfigurationManager.UiLogFile)));

            // The two processes must not share a single log file (cross-process handle contention).
            Assert.NotEqual(
                Path.GetFullPath(ConfigurationManager.LauncherLogFile),
                Path.GetFullPath(ConfigurationManager.UiLogFile));
        }

        [Fact]
        public void LogDirFollowsPlatformConvention()
        {
            var dir = ConfigurationManager.LogDir;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                Assert.StartsWith(Path.GetFullPath(local), Path.GetFullPath(dir), StringComparison.OrdinalIgnoreCase);
                Assert.EndsWith("logs", dir, StringComparison.OrdinalIgnoreCase);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Assert.Contains(Path.Combine("Library", "Logs", "SCALUS"), dir);
            }
            else
            {
                // XDG state: honor $XDG_STATE_HOME, else ~/.local/state; always .../scalus/logs
                Assert.Contains(Path.Combine("scalus", "logs"), dir);
            }
        }

        [Fact]
        public void MinLogLevelAlwaysResolves()
        {
            // Previously null when no appsettings.json was present (Serilog then defaulted to
            // Information). It now always resolves to a concrete level so Debug-by-default is real.
            var level = ConfigurationManager.MinLogLevel;
            Assert.True(level.HasValue);
            Assert.True(Enum.IsDefined<LogEventLevel>(level.Value));
        }
    }
}
