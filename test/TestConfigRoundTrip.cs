using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using OneIdentity.Scalus;
using OneIdentity.Scalus.Dto;
using OneIdentity.Scalus.Util;

namespace OneIdentity.Scalus.Test
{
    // Verifies the "edit + save leaves untouched fields exactly as they were" round-trip claim on
    // realistic data. The save path (ScalusApiConfiguration.ValidateAndSave) serializes with the
    // shared ScalusJson options (camelCase + indented) and writes the file; loading
    // (ScalusConfigurationBase.Load) reads + deserializes + migrates. This drives that same
    // serialize -> write -> Load pipeline against an app that sets PostProcessingExec/Args (which
    // the editor never renders) and an app that sets neither, and asserts nothing is dropped,
    // mangled, or invented.
    public class TestConfigRoundTrip
    {
        private static readonly string[] ExpectedRdpSignArgs = { "/sha256", "%Thumbprint%", "%GeneratedFile%" };

        private static string FixturePath =>
            Path.Combine(AppContext.BaseDirectory, "Fixtures", "RoundTrip", "real-config.json");

        private sealed class DiskLoader : ScalusConfigurationBase
        {
            public ScalusConfig LoadFrom(string path) => Load(path);
        }

        // Mirrors ScalusApiConfiguration.ValidateAndSave: how the configuration is persisted to disk.
        private static string Save(ScalusConfig config) => ScalusJson.Serialize(config);

        // Persist a config the way SaveConfiguration does, then reopen it the way the app does on launch.
        private static ScalusConfig SaveAndReload(ScalusConfig config)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "scalus-rt-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                var path = Path.Combine(tempDir, "SCALUS.json");
                File.WriteAllText(path, Save(config));
                return new DiskLoader().LoadFrom(path);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        private static ParserConfig Parser(ScalusConfig config, string appId) =>
            config.Applications.Single(a => a.Id == appId).Parser;

        [Fact]
        public void SaveReloadPreservesPostProcessingExactly()
        {
            var original = new DiskLoader().LoadFrom(FixturePath);
            var reloaded = SaveAndReload(original);

            var rdp = Parser(reloaded, "rdp-signed");
            Assert.Equal("%AppData%\\rdpsign.exe", rdp.PostProcessingExec);
            Assert.Equal(ExpectedRdpSignArgs, rdp.PostProcessingArgs);

            // The rest of that parser is untouched too.
            Assert.Equal("full address:s:%Host%\nusername:s:%User%\nscreen mode id:i:2", rdp.TemplateContent);
            Assert.Equal(".rdp", rdp.TemplateExtension);
        }

        [Fact]
        public void SaveReloadAddsNothingSpuriousToPlainApplications()
        {
            var original = new DiskLoader().LoadFrom(FixturePath);
            var reloaded = SaveAndReload(original);

            var ssh = Parser(reloaded, "ssh-plain");
            Assert.False(ssh.HasTemplate, "A command-only app must not gain a template");
            Assert.Null(ssh.TemplateContent);
            Assert.Null(ssh.TemplateExtension);
            Assert.True(string.IsNullOrEmpty(ssh.PostProcessingExec), "No post-processing exec should be invented");
            Assert.Null(ssh.PostProcessingArgs);
        }

        [Fact]
        public void SaveReloadIsIdempotent()
        {
            var original = new DiskLoader().LoadFrom(FixturePath);
            var reloaded = SaveAndReload(original);

            // A save cycle on an already-current config must be a no-op at the byte level.
            Assert.Equal(Save(original), Save(reloaded));
        }

        [Fact]
        public void TrivialEditLeavesOtherFieldsUntouched()
        {
            var config = new DiskLoader().LoadFrom(FixturePath);

            // Edit something trivial on the plain app, exactly like changing a name in the editor.
            config.Applications.Single(a => a.Id == "ssh-plain").Name = "OpenSSH (renamed)";

            var reloaded = SaveAndReload(config);

            Assert.Equal("OpenSSH (renamed)", reloaded.Applications.Single(a => a.Id == "ssh-plain").Name);

            // The unrelated app's post-processing is still exactly as it was.
            var rdp = Parser(reloaded, "rdp-signed");
            Assert.Equal("%AppData%\\rdpsign.exe", rdp.PostProcessingExec);
            Assert.Equal(ExpectedRdpSignArgs, rdp.PostProcessingArgs);

            // And the edited app still didn't sprout a template.
            Assert.False(Parser(reloaded, "ssh-plain").HasTemplate);
        }
    }
}
