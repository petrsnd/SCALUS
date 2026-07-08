// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ElevationCommand.cs" company="One Identity Inc.">
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
    using System.Linq;

    /// <summary>
    /// Builds the argument vector that an elevated <c>scalus</c> invocation uses to mutate the
    /// machine layer. Shared by the Windows (runas) and Linux (pkexec/sudo) elevators so the two
    /// stay in lock-step. The command always carries <c>-r</c> (root/all-users) plus the schemes.
    /// </summary>
    internal static class ElevationCommand
    {
        /// <summary>
        /// Builds the launcher arguments (everything after the executable), e.g.
        /// <c>[register, -f, -r, -p, rdp, ssh]</c> or <c>[unregister, -r, -p, rdp]</c>.
        /// </summary>
        public static IReadOnlyList<string> VerbArgs(string verb, IEnumerable<string> schemes)
        {
            var list = new List<string> { verb };
            if (string.Equals(verb, "register", StringComparison.OrdinalIgnoreCase))
            {
                list.Add("-f");
            }

            list.Add("-r");
            list.Add("-p");
            list.AddRange(schemes.Where(s => !string.IsNullOrWhiteSpace(s)));
            return list;
        }

        /// <summary>
        /// Resolves the executable to run and its argument list, accounting for the dev-tree case
        /// where the canonical launcher is a managed <c>.dll</c> that must be run through
        /// <c>dotnet</c>. In production the launcher is a native <c>scalus[.exe]</c>.
        /// </summary>
        public static (string Exe, IReadOnlyList<string> Args) Resolve(string verb, IEnumerable<string> schemes)
        {
            var launcher = Constants.GetLauncherBinaryPath();
            var verbArgs = VerbArgs(verb, schemes);
            if (launcher.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                var args = new List<string> { launcher };
                args.AddRange(verbArgs);
                return (Constants.DotNetPath(), args);
            }

            return (launcher, verbArgs);
        }

        /// <summary>
        /// Renders a resolved command as a single copy/paste string, quoting tokens that contain
        /// whitespace. Used for the "run this in a terminal" fallback.
        /// </summary>
        public static string ToDisplayString(string exe, IReadOnlyList<string> args)
        {
            var parts = new List<string> { Quote(exe) };
            parts.AddRange(args.Select(Quote));
            return string.Join(' ', parts);
        }

        private static string Quote(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "\"\"";
            }

            return value.Contains(' ') ? $"\"{value}\"" : value;
        }
    }
}
