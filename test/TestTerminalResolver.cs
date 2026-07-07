using System.Collections.Generic;
using System.IO;
using System.Linq;
using OneIdentity.Scalus.Dto;
using OneIdentity.Scalus.Platform;
using OneIdentity.Scalus.Util;
using Xunit;

namespace OneIdentity.Scalus.Test
{
    // Covers the preferred-terminal resolver's platform-independent decision logic (the pure static
    // helpers that WrapWindows / the Linux arg builder rely on) plus a config round-trip of the two
    // new fields (ScalusConfig.PreferredTerminal, ParserConfig.RunInTerminal). The registry / process
    // side of the resolver is intentionally not exercised here — the decision logic is factored out so
    // it can be validated without touching the machine.
    public class TestTerminalResolver
    {
        private const string WtClsid = TerminalResolver.WindowsTerminalTerminalClsid;
        private const string NullClsid = "{00000000-0000-0000-0000-000000000000}";
        private const string ConhostClsid = "{B23D10C0-E52E-411E-9D5B-C09FDF709C7D}";
        private const string WtPath = @"C:\Users\test\AppData\Local\Microsoft\WindowsApps\wt.exe";

        [Fact]
        public void Auto_WtIsDefault_UsesWindowsTerminal()
        {
            Assert.True(TerminalResolver.ShouldUseWindowsTerminal("auto", WtClsid, wtAvailable: true));
        }

        [Fact]
        public void Auto_ConhostIsDefault_RespectsConhost()
        {
            Assert.False(TerminalResolver.ShouldUseWindowsTerminal("auto", ConhostClsid, wtAvailable: true));
        }

        [Fact]
        public void Auto_LetWindowsDecide_PrefersWtWhenInstalled()
        {
            Assert.True(TerminalResolver.ShouldUseWindowsTerminal("auto", NullClsid, wtAvailable: true));
            Assert.True(TerminalResolver.ShouldUseWindowsTerminal("auto", null, wtAvailable: true));
        }

        [Fact]
        public void ExplicitConhost_NeverUsesWindowsTerminal_EvenWhenWtIsDefault()
        {
            Assert.False(TerminalResolver.ShouldUseWindowsTerminal("conhost", WtClsid, wtAvailable: true));
        }

        [Fact]
        public void ExplicitWindowsTerminal_UsesWtEvenWhenConhostIsDefault()
        {
            Assert.True(TerminalResolver.ShouldUseWindowsTerminal("windows-terminal", ConhostClsid, wtAvailable: true));
        }

        [Fact]
        public void WtUnavailable_AlwaysFalse()
        {
            Assert.False(TerminalResolver.ShouldUseWindowsTerminal("windows-terminal", WtClsid, wtAvailable: false));
            Assert.False(TerminalResolver.ShouldUseWindowsTerminal("auto", WtClsid, wtAvailable: false));
        }

        [Fact]
        public void WrapWindows_WhenWtChosen_PrependsWtWithInnerExecAndArgs()
        {
            var inner = new[] { "-l", "alice", "host" };
            var cmd = TerminalResolver.WrapWindows(@"C:\Windows\System32\OpenSSH\ssh.exe", inner, "auto", WtClsid, WtPath);

            var expected = new[] { @"C:\Windows\System32\OpenSSH\ssh.exe", "-l", "alice", "host" };
            Assert.Equal(WtPath, cmd.Exec);
            Assert.Equal(expected, cmd.Args.ToArray());
        }

        [Fact]
        public void WrapWindows_WhenWtNotAvailable_ReturnsInnerUnchanged()
        {
            var inner = new[] { "-l", "alice", "host" };
            var cmd = TerminalResolver.WrapWindows(@"C:\ssh.exe", inner, "auto", WtClsid, windowsTerminalPath: null);

            Assert.Equal(@"C:\ssh.exe", cmd.Exec);
            Assert.Equal(inner, cmd.Args.ToArray());
        }

        [Fact]
        public void WrapWindows_WhenConhostChosen_ReturnsInnerUnchanged()
        {
            var inner = new[] { "host" };
            var cmd = TerminalResolver.WrapWindows(@"C:\ssh.exe", inner, "conhost", WtClsid, WtPath);

            Assert.Equal(@"C:\ssh.exe", cmd.Exec);
            Assert.Equal(inner, cmd.Args.ToArray());
        }

        [Fact]
        public void BuildLinuxArgs_GnomeTerminalFamily_UsesDoubleDash()
        {
            var inner = new[] { "-l", "bob", "host" };
            var args = TerminalResolver.BuildLinuxArgs("/usr/bin/gnome-terminal", "ssh", inner);
            var expected = new[] { "--", "ssh", "-l", "bob", "host" };
            Assert.Equal(expected, args.ToArray());
        }

        [Fact]
        public void BuildLinuxArgs_OtherTerminal_UsesDashE()
        {
            var inner = new[] { "host" };
            var args = TerminalResolver.BuildLinuxArgs("/usr/bin/xterm", "ssh", inner);
            var expected = new[] { "-e", "ssh", "host" };
            Assert.Equal(expected, args.ToArray());
        }

        [Fact]
        public void ConfigRoundTrip_PreservesPreferredTerminalAndRunInTerminal()
        {
            var config = new ScalusConfig
            {
                PreferredTerminal = "windows-terminal",
                Protocols = new List<ProtocolMapping>
                {
                    new () { Protocol = "ssh", AppId = "ssh-app" },
                },
                Applications = new List<ApplicationConfig>
                {
                    new ()
                    {
                        Id = "ssh-app",
                        Name = "SSH",
                        Protocol = "ssh",
                        Exec = "/usr/bin/ssh",
                        Args = new List<string> { "-l", "%User%", "%Host%" },
                        Parser = new ParserConfig
                        {
                            ParserId = "ssh",
                            Options = new List<string>(),
                            RunInTerminal = true,
                        },
                    },
                },
            };

            var json = ScalusJson.Serialize(config);
            var reloaded = ScalusJson.Deserialize(json);

            Assert.Equal("windows-terminal", reloaded.PreferredTerminal);
            Assert.True(reloaded.Applications.Single(a => a.Id == "ssh-app").Parser.RunInTerminal);
        }

        [Fact]
        public void ConfigRoundTrip_DefaultRunInTerminalIsFalseAndOmitted()
        {
            var config = new ScalusConfig
            {
                Applications = new List<ApplicationConfig>
                {
                    new ()
                    {
                        Id = "rdp-app",
                        Name = "RDP",
                        Protocol = "rdp",
                        Exec = "mstsc.exe",
                        Parser = new ParserConfig { ParserId = "rdp", Options = new List<string>() },
                    },
                },
            };

            var json = ScalusJson.Serialize(config);

            // RunInTerminal defaults to false and is written with WhenWritingDefault, so it should not
            // appear for an ordinary (non-terminal) application.
            Assert.DoesNotContain("RunInTerminal", json);
            Assert.False(ScalusJson.Deserialize(json).Applications.Single().Parser.RunInTerminal);
        }
    }
}
