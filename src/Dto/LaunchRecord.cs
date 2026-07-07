// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LaunchRecord.cs" company="One Identity Inc.">
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
    using System;
    using System.Text.Json.Serialization;

    // A structured record written once per launch attempt (success OR failure) by the one-shot
    // launcher. Each launch writes its own file (no cross-process append contention), and the
    // configuration UI aggregates them for the Recent launches view. Everything here is safe to
    // persist: the URL is redacted and the token-bearing argument list is deliberately omitted.
    public class LaunchRecord
    {
        // Correlation id shared with the launcher log lines emitted during this launch.
        public string LaunchId { get; set; }

        public DateTime TimestampUtc { get; set; }

        // Which binary handled the launch (scalus vs scalus-ui) — either can be the registered handler.
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string LauncherBinary { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Protocol { get; set; }

        // Redacted request URL (one-time token masked).
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Url { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string ApplicationId { get; set; }

        // Resolved executable path (no arguments — args may carry the token).
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Command { get; set; }

        // Stable outcome keyword: spawned | preview | config-error | spawn-failed |
        // post-execute-error | error.
        public string Outcome { get; set; }

        public bool Success { get; set; }

        // Process exit code when known (null for fire-and-forget spawns).
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? ExitCode { get; set; }

        // Redacted failure detail; null on success.
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Error { get; set; }

        public long DurationMs { get; set; }
    }
}
