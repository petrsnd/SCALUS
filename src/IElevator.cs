// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IElevator.cs" company="One Identity Inc.">
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
    using System.Collections.Generic;

    /// <summary>
    /// Elevates a single all-users (machine-layer) registration mutation. Reads never elevate;
    /// only writes to the machine layer do. The elevated work is delegated to the canonical
    /// <c>scalus</c> launcher run with <c>--root</c>, so the UI itself never runs elevated.
    /// </summary>
    public interface IElevator
    {
        /// <summary>
        /// Gets a value indicating whether this platform supports an all-users registration at all.
        /// macOS returns <c>false</c>: LaunchServices has no supported machine-wide URL-scheme handler.
        /// </summary>
        bool CanElevate { get; }

        /// <summary>
        /// Runs the canonical scalus launcher, elevated, to register or unregister the given schemes
        /// in the machine layer.
        /// </summary>
        /// <param name="verb">Either <c>register</c> or <c>unregister</c>.</param>
        /// <param name="schemes">The URL schemes to (un)register machine-wide.</param>
        /// <returns>The outcome, including a copy/paste fallback command when no GUI elevation exists.</returns>
        ElevationResult Run(string verb, IEnumerable<string> schemes);
    }

    /// <summary>
    /// Outcome of an <see cref="IElevator.Run"/> call.
    /// </summary>
    public sealed class ElevationResult
    {
        /// <summary>Gets a value indicating whether the elevated operation completed successfully.</summary>
        public bool Success { get; init; }

        /// <summary>Gets a value indicating whether the user dismissed the elevation prompt.</summary>
        public bool Cancelled { get; init; }

        /// <summary>
        /// Gets the exact command a user can run manually (e.g. <c>sudo scalus register -r ...</c>)
        /// when no graphical elevation mechanism is available. Null when not applicable.
        /// </summary>
        public string ManualCommand { get; init; }

        /// <summary>Gets a human-readable error message when <see cref="Success"/> is false.</summary>
        public string Error { get; init; }
    }
}
