// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ITerminalResolver.cs" company="One Identity Inc.">
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
    using System.Collections.Generic;

    /// <summary>
    /// Resolves how a terminal-based launch (SSH/telnet clients) should be hosted in the end user's
    /// preferred terminal, per platform.
    /// </summary>
    public interface ITerminalResolver
    {
        /// <summary>
        /// Build the exec+args that runs <paramref name="innerExec"/> (with <paramref name="innerArgs"/>)
        /// inside the preferred terminal. Falls back to the inner command unchanged when no terminal
        /// wrapping applies (e.g. the user chose the legacy console, or no terminal was found).
        /// </summary>
        /// <param name="innerExec">The interactive program to run (e.g. ssh).</param>
        /// <param name="innerArgs">Arguments for the interactive program.</param>
        /// <param name="preference">Preferred terminal id, or null/"auto" to detect.</param>
        /// <returns>The command SCALUS should actually spawn.</returns>
        TerminalCommand Wrap(string innerExec, IReadOnlyList<string> innerArgs, string preference);

        /// <summary>
        /// Enumerate the terminal choices for the current platform. The first entry is always the
        /// "auto" (detect default) option.
        /// </summary>
        /// <returns>Ordered list of terminal options.</returns>
        IReadOnlyList<TerminalOption> GetAvailableTerminals();
    }
}
