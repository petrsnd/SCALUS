// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TestPlatformFilter.cs" company="One Identity Inc.">
//   This software is licensed under the Apache 2.0 open source license.
//   https://github.com/OneIdentity/SCALUS/blob/master/LICENSE
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace OneIdentity.Scalus.Test
{
    using System.Collections.Generic;
    using System.Linq;
    using OneIdentity.Scalus.Dto;
    using OneIdentity.Scalus.Util;
    using Xunit;

    public class TestPlatformFilter
    {
        private static ApplicationConfig App(string id, params Platform[] platforms) => new ApplicationConfig
        {
            Id = id,
            Name = id,
            Protocol = "ssh",
            Platforms = platforms.ToList(),
        };

        [Fact]
        public void KeepsOnlyMatchingPlatform()
        {
            var apps = new List<ApplicationConfig>
            {
                App("win", Platform.Windows),
                App("mac", Platform.Mac),
                App("linux", Platform.Linux),
                App("winmac", Platform.Windows, Platform.Mac),
            };

            var win = PlatformFilter.ForPlatform(apps, Platform.Windows).Select(a => a.Id).ToList();
            Assert.Equal(win, new List<string> { "win", "winmac" });

            var mac = PlatformFilter.ForPlatform(apps, Platform.Mac).Select(a => a.Id).ToList();
            Assert.Equal(mac, new List<string> { "mac", "winmac" });

            var linux = PlatformFilter.ForPlatform(apps, Platform.Linux).Select(a => a.Id).ToList();
            Assert.Equal(linux, new List<string> { "linux" });
        }

        [Fact]
        public void TreatsEmptyOrNullPlatformsAsUniversal()
        {
            var apps = new List<ApplicationConfig>
            {
                App("none"),
                new ApplicationConfig { Id = "nullp", Name = "nullp", Protocol = "ssh", Platforms = null },
                App("win", Platform.Windows),
            };

            var linux = PlatformFilter.ForPlatform(apps, Platform.Linux).Select(a => a.Id).ToList();
            Assert.Equal(linux, new List<string> { "none", "nullp" });
        }

        [Fact]
        public void NullInputYieldsEmptyList()
        {
            Assert.Empty(PlatformFilter.ForPlatform(null, Platform.Windows));
        }
    }
}
