// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ScalusSettings.cs" company="One Identity Inc.">
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
    using System.Text.Json.Serialization;

    // User-editable preferences that live in the per-user, writable SCALUS.json (not in the
    // ship-time appsettings.json). Both the CLI launcher and the configuration UI read these from
    // the single per-user config file they already share, so a change made in the UI's Settings tab
    // is honored by the next one-shot launcher. Every member is optional; an absent value falls back
    // to the code default (Debug logging, console output off).
    public class ScalusSettings
    {
        // Minimum Serilog level name (e.g. "Verbose", "Debug", "Information", "Warning", "Error").
        // Null/empty means use the code default (Debug).
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string LogLevel { get; set; }

        // Whether the launcher also writes log output to the console. Null means the code default (off).
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Console { get; set; }
    }
}
