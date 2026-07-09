// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlatformFilter.cs" company="One Identity Inc.">
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

namespace OneIdentity.Scalus.Util
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using OneIdentity.Scalus.Dto;

    // Single source of truth for "which applications belong on this OS". The shipped seed
    // (SCALUS.json) lists every application across all platforms, each tagged with the
    // Platforms it is valid on; seeding filters that master list down to the running OS so a
    // fresh configuration never offers, for example, a macOS client on Windows. This replaces
    // the old per-platform seed files (scripts/Win|Osx|Linux/SCALUS.json), which duplicated the
    // Platforms metadata and had to be kept in sync by hand.
    public static class PlatformFilter
    {
        // The Platform enum value for the OS this process is running on.
        public static Platform Current
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return Platform.Windows;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return Platform.Mac;
                }

                return Platform.Linux;
            }
        }

        // Keep only the applications valid for the given platform. An application with no
        // Platforms listed is treated as universal (kept) so a hand-edited or imported config is
        // never silently emptied by the filter.
        public static List<ApplicationConfig> ForPlatform(IEnumerable<ApplicationConfig> applications, Platform platform)
        {
            if (applications == null)
            {
                return new List<ApplicationConfig>();
            }

            return applications
                .Where(a => a.Platforms == null || a.Platforms.Count == 0 || a.Platforms.Contains(platform))
                .ToList();
        }

        // Convenience overload that filters to the OS this process is running on.
        public static List<ApplicationConfig> ForCurrentPlatform(IEnumerable<ApplicationConfig> applications)
            => ForPlatform(applications, Current);
    }
}
