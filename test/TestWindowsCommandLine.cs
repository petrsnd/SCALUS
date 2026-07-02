using Xunit;
using OneIdentity.Scalus;

namespace OneIdentity.Scalus.Test
{
    // Tests for the handler-command detection that lets SCALUS recognise when a
    // protocol is registered to a *different* binary (for example a stale install
    // under Program Files) rather than the currently running one. This is the logic
    // behind WindowsProtocolRegistrar.IsScalusRegistered, which must report "not
    // registered" so registration takes the association over instead of silently
    // deferring to the old executable that a browser like Edge would launch.
    public class TestWindowsCommandLine
    {
        private const string DevBinary = @"C:\work\SCALUS\src\Scalus.Ui\bin\Debug\net10.0\scalus-ui.exe";
        private const string StaleBinary = @"C:\Program Files\SCALUS\scalus.exe";

        [Fact]
        public void MatchesCommandThatLaunchesTheSameBinary()
        {
            var command = $"{DevBinary} launch -u \"%1\"";
            Assert.True(WindowsCommandLine.InvokesBinary(command, DevBinary));
        }

        [Fact]
        public void DoesNotMatchCommandThatLaunchesADifferentBinary()
        {
            // The stale Program Files command still mentions "scalus" but resolves to a
            // different executable, so it must not count as our registration.
            var command = $"{StaleBinary} launch -u \"%1\"";
            Assert.False(WindowsCommandLine.InvokesBinary(command, DevBinary));
        }

        [Fact]
        public void MatchesWhenBinaryPathIsQuoted()
        {
            var quoted = @"C:\Program Files\SCALUS\scalus.exe";
            var command = $"\"{quoted}\" launch -u \"%1\"";
            Assert.True(WindowsCommandLine.InvokesBinary(command, quoted));
        }

        [Fact]
        public void ComparisonIsCaseInsensitive()
        {
            var command = $"{DevBinary.ToUpperInvariant()} launch -u \"%1\"";
            Assert.True(WindowsCommandLine.InvokesBinary(command, DevBinary));
        }

        [Fact]
        public void ReturnsFalseForEmptyCommand()
        {
            Assert.False(WindowsCommandLine.InvokesBinary(string.Empty, DevBinary));
            Assert.False(WindowsCommandLine.InvokesBinary(null, DevBinary));
        }
    }
}
