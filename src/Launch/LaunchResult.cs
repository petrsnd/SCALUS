// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LaunchResult.cs" company="One Identity Inc.">
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
    // The honest set of outcomes the launcher can actually observe. The launcher spawns the client
    // and exits, so it can distinguish "we started it / we couldn't start it" but not "the session
    // ultimately succeeded".
    internal enum LaunchOutcome
    {
        // Ran in preview mode; nothing was spawned.
        Preview,

        // The client process was started.
        Spawned,

        // The configured command does not exist / no runnable app was resolved.
        ConfigError,

        // The command exists but the process failed to start.
        SpawnFailed,

        // Post-spawn processing (e.g. observed a non-zero exit before we detached) failed.
        PostExecuteError,
    }

    // In-memory result handed back from ProtocolHandler.Run to Launch.Application, which owns the
    // per-launch record lifecycle. Not serialized directly (see Dto.LaunchRecord for the on-disk form).
    internal sealed class LaunchResult
    {
        public LaunchOutcome Outcome { get; set; }

        public int? ExitCode { get; set; }

        // Failure message (raw).
        public string Error { get; set; }

        public string ApplicationId { get; set; }

        public string Command { get; set; }

        // Full, already-joined argument list for the spawned command (raw — no redaction).
        public string Args { get; set; }

        // Path of the generated file the parser materialized (e.g. the .rdp file), when any. For a real
        // launch this is the persisted path inside the launches directory; null when no file is generated.
        public string GeneratedFile { get; set; }

        public bool Success => Outcome is LaunchOutcome.Spawned or LaunchOutcome.Preview;

        public static string OutcomeKeyword(LaunchOutcome outcome) => outcome switch
        {
            LaunchOutcome.Preview => "preview",
            LaunchOutcome.Spawned => "spawned",
            LaunchOutcome.ConfigError => "config-error",
            LaunchOutcome.SpawnFailed => "spawn-failed",
            LaunchOutcome.PostExecuteError => "post-execute-error",
            _ => "error",
        };
    }
}
