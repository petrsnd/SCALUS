// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IOsServices.cs" company="One Identity Inc.">
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
    using System.Diagnostics;

    public interface IOsServices
    {
        /// <summary>
        /// Open URL with OS default command (start, open, explorer) etc.
        /// </summary>
        /// <param name="url">URL to open</param>
        /// <returns>Process</returns>
        Process OpenDefault(string url);

        /// <summary>
        /// Start a process
        /// </summary>
        /// <param name="binary">Binary</param>
        /// <param name="args">Arguments</param>
        /// <returns>Process</returns>
        Process Execute(string binary, IEnumerable<string> args);

        /// <summary>
        /// Shows a native "launch failed" dialog that offers to open the SCALUS logs deep-linked
        /// to the given launch. Clicking the button spawns the desktop UI with
        /// <c>--show-logs=&lt;launchId&gt;</c>. On a headless/unattended host (no display, or a
        /// dialog helper is unavailable) it logs the failure and returns without blocking.
        /// </summary>
        /// <param name="message">A short human-readable description of the failure</param>
        /// <param name="launchId">The launch record id to deep-link the Logs view to</param>
        void ShowLaunchFailure(string message, string launchId);

        /// <summary>
        /// On Windows: displays text in a MessageBox dialog
        /// On Linux: Console.WriteLine
        /// On Mac: Console.WriteLine
        /// </summary>
        /// <param name="message">The message to display</param>
        void ShowMessage(string message);

        /// <summary>
        /// Returns whether the process is running as administrator
        /// </summary>
        /// <returns>Whether the process is running as administrator</returns>
        bool IsAdministrator();

        /// <summary>
        /// Run a command
        /// </summary>
        /// <param name="command">Command to execute</param>
        /// <param name="args">Arguments</param>
        /// <param name="stdOut">Captured standard output</param>
        /// <param name="stdErr">Captured standard error</param>
        /// <returns>Process result code</returns>
        int Execute(string command, IEnumerable<string> args, out string stdOut, out string stdErr);
    }
}
