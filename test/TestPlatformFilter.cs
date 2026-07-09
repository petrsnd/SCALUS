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

        private static ApplicationConfig App(string id, string protocol, params Platform[] platforms) => new ApplicationConfig
        {
            Id = id,
            Name = id,
            Protocol = protocol,
            Platforms = platforms.ToList(),
        };

        [Fact]
        public void ApplyToConfigRemapsDanglingProtocolDefaultToSurvivingApp()
        {
            // Mirrors the shipped master seed: every protocol default points at the Windows client.
            var config = new ScalusConfig
            {
                Applications = new List<ApplicationConfig>
                {
                    App("windows-rdp", "rdp", Platform.Windows),
                    App("mac-rdp", "rdp", Platform.Mac),
                    App("windows-openssh", "ssh", Platform.Windows),
                    App("mac-ssh", "ssh", Platform.Mac),
                },
                Protocols = new List<ProtocolMapping>
                {
                    new ProtocolMapping { Protocol = "rdp", AppId = "windows-rdp" },
                    new ProtocolMapping { Protocol = "ssh", AppId = "windows-openssh" },
                },
            };

            PlatformFilter.ApplyToConfig(config, Platform.Mac);

            Assert.Equal(new List<string> { "mac-rdp", "mac-ssh" }, config.Applications.Select(a => a.Id).ToList());
            Assert.Equal("mac-rdp", config.Protocols.Single(p => p.Protocol == "rdp").AppId);
            Assert.Equal("mac-ssh", config.Protocols.Single(p => p.Protocol == "ssh").AppId);
        }

        [Fact]
        public void ApplyToConfigClearsProtocolDefaultWhenNoAppHandlesItHere()
        {
            var config = new ScalusConfig
            {
                Applications = new List<ApplicationConfig>
                {
                    App("putty-telnet", "telnet", Platform.Windows),
                    App("mac-ssh", "ssh", Platform.Mac),
                },
                Protocols = new List<ProtocolMapping>
                {
                    new ProtocolMapping { Protocol = "telnet", AppId = "putty-telnet" },
                },
            };

            PlatformFilter.ApplyToConfig(config, Platform.Mac);

            Assert.Null(config.Protocols.Single(p => p.Protocol == "telnet").AppId);
        }

        [Fact]
        public void ApplyToConfigLeavesValidDefaultsUnchanged()
        {
            var config = new ScalusConfig
            {
                Applications = new List<ApplicationConfig>
                {
                    App("windows-rdp", "rdp", Platform.Windows),
                    App("windows-openssh", "ssh", Platform.Windows),
                },
                Protocols = new List<ProtocolMapping>
                {
                    new ProtocolMapping { Protocol = "rdp", AppId = "windows-rdp" },
                    new ProtocolMapping { Protocol = "ssh", AppId = "windows-openssh" },
                },
            };

            PlatformFilter.ApplyToConfig(config, Platform.Windows);

            Assert.Equal("windows-rdp", config.Protocols.Single(p => p.Protocol == "rdp").AppId);
            Assert.Equal("windows-openssh", config.Protocols.Single(p => p.Protocol == "ssh").AppId);
        }
    }
}
