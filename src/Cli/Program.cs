// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="One Identity Inc.">
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

namespace OneIdentity.Scalus
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;

    internal sealed partial class Program
    {
        private static int Main(string[] args)
        {
            // scalus.exe is a console-subsystem executable, so when the OS invokes it as the
            // registered protocol handler (e.g. a browser running `scalus.exe launch -u rdp://...`)
            // Windows creates a console window for the process. Detach from that console on the
            // launch hot path so no console window is left flickering on the user's screen. CLI
            // verbs (info, register, --help, ...) keep their console, and `--preview`/`--debug`
            // opt back in for troubleshooting. (OneIdentity/SCALUS issue #131)
            //
            // scalus-ui.exe is a WinExe (no console) and never reaches this entry point, so this
            // is intentionally scoped to the console launcher only.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && ShouldDetachConsole(args))
            {
                try
                {
                    FreeConsole();

                    // The console handles are now invalid; point stdout/stderr at a sink so any
                    // Console writes or Serilog console sinks downstream are harmless no-ops.
                    Console.SetOut(TextWriter.Null);
                    Console.SetError(TextWriter.Null);
                }
                catch
                {
                    // Best-effort: if we can't detach the console we still launch normally.
                }
            }

            // All verb parsing, configuration bootstrap, and execution lives in the shared
            // CommandLineRunner so the CLI (scalus) and the UI executable (scalus-ui), when
            // invoked as a protocol handler, behave identically.
            return CommandLineRunner.Run(args);
        }

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool FreeConsole();

        private static bool ShouldDetachConsole(string[] args)
        {
            // Only the launch verb is ever invoked as an OS protocol handler; every other
            // verb is an interactive CLI command that should keep its console.
            if (args == null || args.Length == 0 ||
                !string.Equals(args[0].Trim(), "launch", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Keep the console when the caller explicitly wants to see output.
            return !args.Any(a =>
                string.Equals(a, "--debug", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(a, "--preview", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(a, "-p", StringComparison.OrdinalIgnoreCase));
        }
    }
}
