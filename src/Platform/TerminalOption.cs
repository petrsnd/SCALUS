// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TerminalOption.cs" company="One Identity Inc.">
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
    /// <summary>
    /// A terminal choice that can be presented to the user (id is stored in ScalusConfig.PreferredTerminal).
    /// </summary>
    public sealed class TerminalOption
    {
        public TerminalOption(string id, string name, bool available)
        {
            Id = id;
            Name = name;
            Available = available;
        }

        // Stable identifier persisted in configuration (e.g. "auto", "windows-terminal", "conhost").
        public string Id { get; }

        // User-facing display name.
        public string Name { get; }

        // Whether this terminal was detected on the current machine (drives UI enable/greying).
        public bool Available { get; }
    }
}
