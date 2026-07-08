// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LinuxElevator.cs" company="One Identity Inc.">
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
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.Versioning;

    /// <summary>
    /// Linux elevation via PolicyKit's <c>pkexec</c> (a graphical authentication prompt, the desktop
    /// analog of Windows UAC) running the canonical <c>scalus register/unregister -r ...</c>. When
    /// <c>pkexec</c> is unavailable (headless/minimal installs) the operation is not attempted
    /// silently; instead the exact <c>sudo scalus ...</c> command is surfaced for the user to run in
    /// a terminal (plain <c>sudo</c> from a GUI has no controlling TTY to read a password).
    /// </summary>
    [SupportedOSPlatform("linux")]
    internal sealed class LinuxElevator : IElevator
    {
        private const string Pkexec = "/usr/bin/pkexec";

        // pkexec: 126 = not authorized / dialog dismissed, 127 = authentication error.
        private const int PkexecNotAuthorized = 126;
        private const int PkexecAuthError = 127;

        public bool CanElevate => true;

        public ElevationResult Run(string verb, IEnumerable<string> schemes)
        {
            var (exe, args) = ElevationCommand.Resolve(verb, schemes);
            var manual = "sudo " + ElevationCommand.ToDisplayString(exe, args);

            if (!File.Exists(Pkexec))
            {
                return new ElevationResult
                {
                    Success = false,
                    ManualCommand = manual,
                    Error = "PolicyKit (pkexec) is not available on this system.",
                };
            }

            try
            {
                var psi = new ProcessStartInfo { FileName = Pkexec, UseShellExecute = false };
                psi.ArgumentList.Add(exe);
                foreach (var a in args)
                {
                    psi.ArgumentList.Add(a);
                }

                using var proc = Process.Start(psi);
                if (proc == null)
                {
                    return new ElevationResult { Success = false, ManualCommand = manual, Error = "Failed to start pkexec." };
                }

                proc.WaitForExit();
                if (proc.ExitCode == 0)
                {
                    return new ElevationResult { Success = true };
                }

                var cancelled = proc.ExitCode == PkexecNotAuthorized || proc.ExitCode == PkexecAuthError;
                return new ElevationResult
                {
                    Success = false,
                    Cancelled = cancelled,
                    ManualCommand = manual,
                    Error = cancelled ? "Authentication was cancelled or denied." : $"pkexec exited with code {proc.ExitCode}.",
                };
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Linux elevation failed");
                return new ElevationResult { Success = false, ManualCommand = manual, Error = ex.Message };
            }
        }
    }
}
