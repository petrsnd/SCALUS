// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TerminalCommand.cs" company="One Identity Inc.">
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
    /// The concrete command SCALUS should spawn to run an interactive program: either the program
    /// itself (no terminal wrapping) or a terminal emulator configured to host it.
    /// </summary>
    public sealed class TerminalCommand
    {
        public TerminalCommand(string exec, IReadOnlyList<string> args)
        {
            Exec = exec;
            Args = args;
        }

        public string Exec { get; }

        public IReadOnlyList<string> Args { get; }
    }
}
