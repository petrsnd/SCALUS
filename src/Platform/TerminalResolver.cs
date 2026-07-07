// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TerminalResolver.cs" company="One Identity Inc.">
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

namespace OneIdentity.Scalus.Platform
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Text;
    using Serilog;

    /// <summary>
    /// Resolves the end user's preferred terminal and wraps interactive launches (SSH/telnet) so they
    /// open in it. A single implementation branches on the current OS at runtime (matching the pattern
    /// used by <see cref="OsServicesBase"/>). The platform decision logic is factored into pure static
    /// helpers so it can be unit-tested without touching the registry or filesystem.
    /// </summary>
    public class TerminalResolver : ITerminalResolver
    {
        // Preferred-terminal id meaning "detect the platform default".
        public const string Auto = "auto";

        // The DelegationTerminal CLSID that Windows writes when Windows Terminal is the default
        // terminal application (HKCU\Console\%%Startup\DelegationTerminal).
        internal const string WindowsTerminalTerminalClsid = "{E12CFF52-A866-4C77-9A90-F570A7AA2C6B}";

        // Windows preferred-terminal ids.
        internal const string WindowsTerminalId = "windows-terminal";
        internal const string ConhostId = "conhost";

        // The all-zero CLSID / "Let Windows decide".
        private const string NullClsid = "{00000000-0000-0000-0000-000000000000}";

        // Linux terminal emulators whose "run this command" convention is the double-dash form
        // (everything after "--" is the command). Everything else defaults to "-e".
        private static readonly HashSet<string> LinuxDoubleDashTerminals = new (StringComparer.OrdinalIgnoreCase)
        {
            "gnome-terminal", "mate-terminal", "tilix", "kgx",
        };

        // Ordered probe list used when auto-detecting a Linux terminal emulator.
        private static readonly string[] LinuxTerminalProbe =
        {
            "x-terminal-emulator", "gnome-terminal", "konsole", "xfce4-terminal", "mate-terminal",
            "tilix", "alacritty", "kitty", "lxterminal", "xterm",
        };

        public TerminalCommand Wrap(string innerExec, IReadOnlyList<string> innerArgs, string preference)
        {
            innerArgs ??= Array.Empty<string>();
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return WrapWindows(innerExec, innerArgs, preference, GetWindowsDelegationTerminalClsid(), GetWindowsTerminalPath());
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return WrapLinux(innerExec, innerArgs, preference);
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return WrapMac(innerExec, innerArgs, preference);
                }
            }
            catch (Exception e)
            {
                Log.Warning($"Failed to resolve preferred terminal ({e.Message}); launching '{innerExec}' directly.");
            }

            return new TerminalCommand(innerExec, innerArgs);
        }

        public IReadOnlyList<TerminalOption> GetAvailableTerminals()
        {
            var options = new List<TerminalOption>
            {
                new (Auto, "Automatic (detect default)", true),
            };

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    options.Add(new TerminalOption(WindowsTerminalId, "Windows Terminal", GetWindowsTerminalPath() != null));
                    options.Add(new TerminalOption(ConhostId, "Windows Console Host (legacy)", true));
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    foreach (var probe in LinuxTerminalProbe)
                    {
                        if (probe == "x-terminal-emulator")
                        {
                            continue;
                        }

                        if (FindOnPath(probe) != null)
                        {
                            options.Add(new TerminalOption(probe, probe, true));
                        }
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    options.Add(new TerminalOption("terminal", "Terminal", true));
                    options.Add(new TerminalOption("iterm", "iTerm", Directory.Exists("/Applications/iTerm.app")));
                }
            }
            catch (Exception e)
            {
                Log.Warning($"Failed to enumerate terminals: {e.Message}");
            }

            return options;
        }

        /// <summary>
        /// Pure decision + composition for Windows. When Windows Terminal should host the command and
        /// wt.exe is available, produce "wt.exe &lt;innerExec&gt; &lt;innerArgs...&gt;"; otherwise return the
        /// inner command unchanged (the OS allocates a legacy console / honors any handoff itself).
        /// </summary>
        internal static TerminalCommand WrapWindows(string innerExec, IReadOnlyList<string> innerArgs, string preference, string delegationTerminalClsid, string windowsTerminalPath)
        {
            innerArgs ??= Array.Empty<string>();
            if (windowsTerminalPath != null && ShouldUseWindowsTerminal(preference, delegationTerminalClsid, wtAvailable: true))
            {
                var args = new List<string>(innerArgs.Count + 1) { innerExec };
                args.AddRange(innerArgs);
                return new TerminalCommand(windowsTerminalPath, args);
            }

            return new TerminalCommand(innerExec, innerArgs);
        }

        /// <summary>
        /// Decide whether a terminal-based launch should be hosted in Windows Terminal. Explicit
        /// preferences win; under "auto" the user's default-terminal choice
        /// (HKCU\Console\%%Startup\DelegationTerminal) is honored, and when that is unset ("Let Windows
        /// decide") Windows Terminal is preferred whenever it is installed.
        /// </summary>
        internal static bool ShouldUseWindowsTerminal(string preference, string delegationTerminalClsid, bool wtAvailable)
        {
            if (!wtAvailable)
            {
                return false;
            }

            var pref = (preference ?? string.Empty).Trim().ToLowerInvariant();
            switch (pref)
            {
                case ConhostId:
                case "legacy":
                case "windows-console":
                    return false;
                case WindowsTerminalId:
                case "wt":
                case "terminal":
                    return true;
            }

            // auto / empty: honor the user's configured default terminal.
            var clsid = (delegationTerminalClsid ?? string.Empty).Trim();
            if (clsid.Equals(WindowsTerminalTerminalClsid, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!string.IsNullOrEmpty(clsid) && !clsid.Equals(NullClsid, StringComparison.OrdinalIgnoreCase))
            {
                // A non-null, non-WT terminal is explicitly selected (e.g. conhost) -> respect it.
                return false;
            }

            // Unset / "Let Windows decide": prefer Windows Terminal since it is installed.
            return true;
        }

        // Compose the emulator argument list. gnome-terminal-family uses "-- cmd args"; the rest use
        // "-e cmd args".
        internal static List<string> BuildLinuxArgs(string terminalPath, string innerExec, IReadOnlyList<string> innerArgs)
        {
            innerArgs ??= Array.Empty<string>();
            var name = Path.GetFileName(terminalPath);
            var args = new List<string>
            {
                LinuxDoubleDashTerminals.Contains(name) ? "--" : "-e",
                innerExec,
            };
            args.AddRange(innerArgs);
            return args;
        }

        // Overridable seam so tests (and the pure WrapWindows) do not depend on the machine registry.
        protected virtual string GetWindowsDelegationTerminalClsid()
        {
            try
            {
                return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ReadWindowsDelegationTerminalClsid() : null;
            }
            catch (Exception e)
            {
                Log.Warning($"Could not read default terminal from registry: {e.Message}");
                return null;
            }
        }

        protected virtual string GetWindowsTerminalPath()
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrEmpty(local))
            {
                var candidate = Path.Combine(local, "Microsoft", "WindowsApps", "wt.exe");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return FindOnPath("wt.exe");
        }

        [SupportedOSPlatform("windows")]
        private static string ReadWindowsDelegationTerminalClsid()
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Console\%%Startup");
            return key?.GetValue("DelegationTerminal") as string;
        }

        private static TerminalCommand WrapLinux(string innerExec, IReadOnlyList<string> innerArgs, string preference)
        {
            var terminal = ResolveLinuxTerminal(preference);
            if (terminal == null)
            {
                Log.Information($"No terminal emulator found; launching '{innerExec}' directly.");
                return new TerminalCommand(innerExec, innerArgs);
            }

            return new TerminalCommand(terminal, BuildLinuxArgs(terminal, innerExec, innerArgs));
        }

        private static string ResolveLinuxTerminal(string preference)
        {
            var pref = (preference ?? string.Empty).Trim();
            if (!string.IsNullOrEmpty(pref) && !pref.Equals(Auto, StringComparison.OrdinalIgnoreCase))
            {
                if (Path.IsPathRooted(pref) && File.Exists(pref))
                {
                    return pref;
                }

                var resolved = FindOnPath(pref);
                if (resolved != null)
                {
                    return resolved;
                }

                Log.Warning($"Preferred terminal '{pref}' not found; falling back to auto-detection.");
            }

            var candidates = new List<string> { "x-terminal-emulator" };
            var envTerminal = Environment.GetEnvironmentVariable("TERMINAL");
            if (!string.IsNullOrEmpty(envTerminal))
            {
                candidates.Add(envTerminal);
            }

            candidates.AddRange(LinuxTerminalProbe.Skip(1));

            foreach (var candidate in candidates)
            {
                var path = Path.IsPathRooted(candidate) && File.Exists(candidate) ? candidate : FindOnPath(candidate);
                if (path != null)
                {
                    return path;
                }
            }

            return null;
        }

        private static TerminalCommand WrapMac(string innerExec, IReadOnlyList<string> innerArgs, string preference)
        {
            var app = ResolveMacTerminalApp(preference);

            // Terminal.app / iTerm cannot run an arbitrary argv via "open"; write a short executable
            // .command script and open it with the chosen app.
            var scriptPath = WriteMacCommandScript(innerExec, innerArgs);
            return new TerminalCommand("/usr/bin/open", new List<string> { "-a", app, scriptPath });
        }

        private static string ResolveMacTerminalApp(string preference)
        {
            var pref = (preference ?? string.Empty).Trim().ToLowerInvariant();
            if (pref == "iterm" || pref == "iterm2")
            {
                return "iTerm";
            }

            if (pref == "terminal")
            {
                return "Terminal";
            }

            // auto: prefer iTerm when installed, else the built-in Terminal.
            return Directory.Exists("/Applications/iTerm.app") ? "iTerm" : "Terminal";
        }

        [SupportedOSPlatform("macos")]
        [SupportedOSPlatform("linux")]
        private static string WriteMacCommandScript(string innerExec, IReadOnlyList<string> innerArgs)
        {
            innerArgs ??= Array.Empty<string>();
            var sb = new StringBuilder();
            sb.Append("#!/bin/sh\n");
            sb.Append("exec ").Append(ShellQuote(innerExec));
            foreach (var arg in innerArgs)
            {
                sb.Append(' ').Append(ShellQuote(arg));
            }

            sb.Append('\n');

            var path = Path.Combine(Path.GetTempPath(), $"scalus-{Guid.NewGuid():N}.command");
            File.WriteAllText(path, sb.ToString());
            const UnixFileMode Mode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                                      UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                                      UnixFileMode.OtherRead | UnixFileMode.OtherExecute;
            File.SetUnixFileMode(path, Mode);
            return path;
        }

        // Single-quote for POSIX shells, escaping embedded single quotes.
        private static string ShellQuote(string value)
        {
            value ??= string.Empty;
            return "'" + value.Replace("'", "'\\''") + "'";
        }

        // Resolve a command name against PATH (and PATHEXT on Windows). Returns the full path or null.
        private static string FindOnPath(string command)
        {
            if (string.IsNullOrEmpty(command))
            {
                return null;
            }

            if (Path.IsPathRooted(command))
            {
                return File.Exists(command) ? command : null;
            }

            var pathVar = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(pathVar))
            {
                return null;
            }

            var extensions = new List<string> { string.Empty };
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !command.Contains('.'))
            {
                var pathext = Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.CMD;.BAT";
                extensions.AddRange(pathext.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries));
            }

            foreach (var dir in pathVar.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                foreach (var ext in extensions)
                {
                    string candidate;
                    try
                    {
                        candidate = Path.Combine(dir.Trim(), command + ext);
                    }
                    catch (ArgumentException)
                    {
                        continue;
                    }

                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
            }

            return null;
        }
    }
}
