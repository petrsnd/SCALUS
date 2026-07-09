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
    using System;
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

        // Filter a whole configuration down to the given platform: keep only the applications
        // valid here AND fix up the protocol defaults so none point at an application that was
        // just filtered away. The shipped master seed maps every protocol to its Windows client
        // (e.g. rdp -> windows-rdp); on macOS/Linux those apps are removed by the filter, which
        // would otherwise leave a dangling default that silently fails to launch. When a
        // protocol's configured app disappears, fall back to the first surviving application that
        // handles the same protocol on this platform (rdp -> mac-rdp, ssh -> mac-ssh, ...), or
        // clear the mapping when nothing here handles it (e.g. telnet has no macOS client).
        public static void ApplyToConfig(ScalusConfig config, Platform platform)
        {
            if (config == null)
            {
                return;
            }

            config.Applications = ForPlatform(config.Applications, platform);

            if (config.Protocols == null)
            {
                return;
            }

            var available = new HashSet<string>(
                config.Applications
                    .Select(a => a.Id)
                    .Where(id => !string.IsNullOrEmpty(id)),
                StringComparer.OrdinalIgnoreCase);

            foreach (var mapping in config.Protocols)
            {
                if (string.IsNullOrEmpty(mapping.AppId) || available.Contains(mapping.AppId))
                {
                    continue;
                }

                var replacement = config.Applications
                    .FirstOrDefault(a => string.Equals(a.Protocol, mapping.Protocol, StringComparison.OrdinalIgnoreCase));
                mapping.AppId = replacement?.Id;
            }
        }

        // Convenience overload that filters the configuration to the OS this process is running on.
        public static void ApplyToCurrentPlatform(ScalusConfig config)
            => ApplyToConfig(config, Current);
    }
}
