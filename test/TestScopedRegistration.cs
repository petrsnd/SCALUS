using OneIdentity.Scalus.Dto;
using OneIdentity.Scalus.Platform;
using Xunit;

namespace OneIdentity.Scalus.Test
{
    // Detection must report the currently-selected scope (per-user vs all-users / HKCU vs HKLM),
    // not just the default. The owner's requirement is "registrations only report on the mode
    // selected", so Registration must push the requested rootMode onto every registrar before it
    // reads status. These tests pin that propagation with a registrar that records what scope it
    // was asked about.
    public class TestScopedRegistration
    {
        [Fact]
        public void GetStatus_AllUsers_ReadsMachineScope()
        {
            var recorder = new ScopeRecordingRegistrar(isScalus: true);
            var registration = new Registration(new[] { (IProtocolRegistrar)recorder }, null, null);

            registration.GetStatus("rdp", rootMode: true);

            Assert.True(recorder.ObservedRootMode);
        }

        [Fact]
        public void GetStatus_PerUser_ReadsUserScope()
        {
            var recorder = new ScopeRecordingRegistrar(isScalus: true);
            var registration = new Registration(new[] { (IProtocolRegistrar)recorder }, null, null);

            registration.GetStatus("rdp", rootMode: false);

            Assert.False(recorder.ObservedRootMode);
        }

        [Fact]
        public void IsRegistered_AllUsers_ReadsMachineScope()
        {
            var recorder = new ScopeRecordingRegistrar(isScalus: true);
            var registration = new Registration(new[] { (IProtocolRegistrar)recorder }, null, null);

            registration.IsRegistered("rdp", rootMode: true);

            Assert.True(recorder.ObservedRootMode);
        }

        // Records the RootMode value in effect at the moment status is read, so a test can assert
        // that the requested scope was propagated before the read.
        private sealed class ScopeRecordingRegistrar : IProtocolRegistrar
        {
            private readonly bool isScalus;

            public ScopeRecordingRegistrar(bool isScalus)
            {
                this.isScalus = isScalus;
            }

            public IOsServices OsServices => null;

            public bool UseSudo { get; set; }

            public bool RootMode { get; set; }

            public bool? ObservedRootMode { get; private set; }

            public string Name => "scope-recorder";

            public string GetRegisteredCommand(string protocol) => null;

            public bool IsScalusRegistered(string protocol)
            {
                ObservedRootMode = RootMode;
                return isScalus;
            }

            public bool Unregister(string protocol) => true;

            public bool Register(string protocol) => true;

            public bool ReplaceRegistration(string protocol) => true;
        }
    }
}
