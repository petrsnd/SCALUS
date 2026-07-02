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
    // only "ours" when one of its tokens resolves to the currently running binary; a
    // command left behind by a previous install (for example an older copy under
    // Program Files) mentions the scalus name but launches a different executable, so it
    // must not be treated as an active registration.
    internal static class WindowsCommandLine
    {
        public static bool InvokesThisBinary(string command)
        {
            string ourBinary;
            try
            {
                ourBinary = Path.GetFullPath(Constants.GetBinaryPath());
            }
            catch (Exception)
            {
                return false;
            }

            return InvokesBinary(command, ourBinary);
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
    }
}
