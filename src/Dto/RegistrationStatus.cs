// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RegistrationStatus.cs" company="One Identity Inc.">
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

namespace OneIdentity.Scalus.Dto
{
    /// <summary>
    /// The current OS-registration state for a single protocol scheme, as surfaced to the
    /// configuration UI. <see cref="State"/> is one of "registered" (this SCALUS is the handler),
    /// "conflict" (a different application is the handler) or "unregistered" (no handler).
    /// The <see cref="Program"/>/<see cref="Path"/>/<see cref="Command"/> fields are only
    /// populated for the conflict state and describe the foreign handler.
    /// </summary>
    public sealed class RegistrationStatus
    {
        public const string Registered = "registered";
        public const string Conflict = "conflict";
        public const string Unregistered = "unregistered";

        public string Protocol { get; set; }

        public string State { get; set; } = Unregistered;

        /// <summary>Gets or sets a friendly name for the conflicting application (conflict only).</summary>
        public string Program { get; set; }

        /// <summary>Gets or sets the resolved executable path of the conflicting handler (conflict only).</summary>
        public string Path { get; set; }

        /// <summary>Gets or sets the raw registered command line of the conflicting handler (conflict only).</summary>
        public string Command { get; set; }
    }
}
