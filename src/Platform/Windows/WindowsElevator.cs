// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WindowsElevator.cs" company="One Identity Inc.">
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
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Runtime.Versioning;

    /// <summary>
    /// Windows elevation via ShellExecute verb <c>runas</c>: a single UAC prompt runs the canonical
    /// <c>scalus.exe register/unregister -r ...</c> which writes the machine hive (HKLM). The UI itself
    /// is never elevated.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal sealed class WindowsElevator : IElevator
    {
        private const int ErrorCancelled = 1223; // ERROR_CANCELLED — user dismissed the UAC prompt.

        public bool CanElevate => true;

        public ElevationResult Run(string verb, IEnumerable<string> schemes)
        {
            var (exe, args) = ElevationCommand.Resolve(verb, schemes);
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = exe,
                    UseShellExecute = true,
                    Verb = "runas",
                    WindowStyle = ProcessWindowStyle.Hidden,
                };
                foreach (var a in args)
                {
                    psi.ArgumentList.Add(a);
                }

                using var proc = Process.Start(psi);
                if (proc == null)
                {
                    return new ElevationResult { Success = false, Error = "Failed to start the elevated process." };
                }

                proc.WaitForExit();
                return proc.ExitCode == 0
                    ? new ElevationResult { Success = true }
                    : new ElevationResult { Success = false, Error = $"Elevated scalus exited with code {proc.ExitCode}." };
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == ErrorCancelled)
            {
                return new ElevationResult { Success = false, Cancelled = true, Error = "Elevation was cancelled." };
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Windows elevation failed");
                return new ElevationResult { Success = false, Error = ex.Message };
            }
        }
    }
}
