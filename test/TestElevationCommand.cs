using OneIdentity.Scalus.Platform;
using Xunit;

namespace OneIdentity.Scalus.Test
{
    // The elevated all-users broker runs a one-shot `scalus register/unregister -r -p <schemes>`.
    // ElevationCommand builds that argument vector, shared by the Windows (runas) and Linux
    // (pkexec/sudo) elevators so both stay identical. These tests pin the flag shape.
    public class TestElevationCommand
    {
        private static readonly string[] RdpSsh = { "rdp", "ssh" };
        private static readonly string[] Rdp = { "rdp" };
        private static readonly string[] RdpBlanksTelnet = { "rdp", "", "  ", "telnet" };
        private static readonly string[] RegisterArgs = { "register", "-f", "-r", "-p", "rdp", "ssh" };
        private static readonly string[] UnregisterArgs = { "unregister", "-r", "-p", "rdp" };
        private static readonly string[] RegisterSkipBlanks = { "register", "-f", "-r", "-p", "rdp", "telnet" };
        private static readonly string[] DisplayArgs = { "register", "-r", "-p", "rdp" };

        [Fact]
        public void Register_IncludesForceRootAndProtocols()
        {
            var args = ElevationCommand.VerbArgs("register", RdpSsh);

            Assert.Equal(RegisterArgs, args);
        }

        [Fact]
        public void Unregister_OmitsForce()
        {
            var args = ElevationCommand.VerbArgs("unregister", Rdp);

            Assert.Equal(UnregisterArgs, args);
        }

        [Fact]
        public void VerbArgs_SkipsBlankSchemes()
        {
            var args = ElevationCommand.VerbArgs("register", RdpBlanksTelnet);

            Assert.Equal(RegisterSkipBlanks, args);
        }

        [Fact]
        public void ToDisplayString_QuotesTokensWithSpaces()
        {
            var display = ElevationCommand.ToDisplayString(
                "C:\\Program Files\\SCALUS\\scalus.exe",
                DisplayArgs);

            Assert.Equal("\"C:\\Program Files\\SCALUS\\scalus.exe\" register -r -p rdp", display);
        }
    }
}
