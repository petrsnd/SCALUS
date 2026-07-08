// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WindowsCommandLine.cs" company="One Identity Inc.">
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
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    // Helpers for reasoning about a registered handler command line. A registration is
    // only "ours" when one of its tokens resolves to a SCALUS launcher (scalus or
    // scalus-ui) living in our own install directory. Either binary is accepted because
    // the CLI (scalus) is now the canonical registered handler but existing installs and
    // the dev tree may still be registered to the GUI (scalus-ui); both are the same
    // product in the same directory. A command left behind by a previous install (an
    // older copy under a *different* directory) mentions the scalus name but launches a
    // different executable, so it must not be treated as an active registration.
    internal static class WindowsCommandLine
    {
        public static bool InvokesThisBinary(string command)
        {
            if (string.IsNullOrEmpty(command))
            {
                return false;
            }

            string installDir;
            try
            {
                installDir = NormalizeDirectory(Constants.GetBinaryDir());
            }
            catch (Exception)
            {
                return false;
            }

            if (string.IsNullOrEmpty(installDir))
            {
                return false;
            }

            foreach (var token in SplitCommandLine(command))
            {
                string full;
                try
                {
                    full = Path.GetFullPath(token);
                }
                catch (Exception)
                {
                    continue;
                }

                if (!IsScalusLauncherName(Path.GetFileNameWithoutExtension(full)))
                {
                    continue;
                }

                string dir;
                try
                {
                    dir = NormalizeDirectory(Path.GetDirectoryName(full));
                }
                catch (Exception)
                {
                    continue;
                }

                if (string.Equals(dir, installDir, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool InvokesBinary(string command, string binaryPath)
        {
            if (string.IsNullOrEmpty(command) || string.IsNullOrEmpty(binaryPath))
            {
                return false;
            }

            foreach (var token in SplitCommandLine(command))
            {
                string full;
                try
                {
                    full = Path.GetFullPath(token);
                }
                catch (Exception)
                {
                    continue;
                }

                if (string.Equals(full, binaryPath, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        // Returns the executable token (the first argument) of a registered command line, or
        // null when the command is empty. Used to describe a conflicting foreign handler.
        public static string GetExecutable(string command)
        {
            if (string.IsNullOrEmpty(command))
            {
                return null;
            }

            foreach (var token in SplitCommandLine(command))
            {
                return token;
            }

            return null;
        }

        public static IEnumerable<string> SplitCommandLine(string command)
        {
            var current = new StringBuilder();
            var inQuotes = false;

            foreach (var ch in command)
            {
                if (ch == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (char.IsWhiteSpace(ch) && !inQuotes)
                {
                    if (current.Length > 0)
                    {
                        yield return current.ToString();
                        current.Clear();
                    }
                }
                else
                {
                    current.Append(ch);
                }
            }

            if (current.Length > 0)
            {
                yield return current.ToString();
            }
        }

        private static bool IsScalusLauncherName(string name) =>
            string.Equals(name, "scalus", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "scalus-ui", StringComparison.OrdinalIgnoreCase);

        private static string NormalizeDirectory(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            return Path.GetFullPath(path)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }
}
