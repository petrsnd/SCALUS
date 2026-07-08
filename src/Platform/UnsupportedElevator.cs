// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UnsupportedElevator.cs" company="One Identity Inc.">
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
    /// Elevator for platforms with no supported machine-wide URL-scheme registration — notably macOS,
    /// where LaunchServices only exposes a strictly per-user default handler. All-users registration
    /// is not offered on these platforms, so <see cref="CanElevate"/> is false and callers should hide
    /// or disable the all-users affordance.
    /// </summary>
    internal sealed class UnsupportedElevator : IElevator
    {
        public bool CanElevate => false;

        public ElevationResult Run(string verb, IEnumerable<string> schemes)
        {
            return new ElevationResult
            {
                Success = false,
                Error = "All-users registration is not supported on this platform.",
            };
        }
    }
}
