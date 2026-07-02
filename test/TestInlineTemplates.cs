using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using OneIdentity.Scalus;
using OneIdentity.Scalus.Dto;
using OneIdentity.Scalus.UrlParser;
using static OneIdentity.Scalus.Dto.ParserConfigDefinitions;

namespace OneIdentity.Scalus.Test
{
    public class TestInlineTemplates
    {
        private const string RdpUrl = "rdp://full+address=s:myhost:3389&username=s:me/";

        private static byte[] GenerateBytes(ParserConfig config, string url = RdpUrl)
        {
            using var sut = new DefaultRdpUrlParser(config);
            var dictionary = sut.Parse(url);
            var generated = dictionary[Token.GeneratedFile];
            Assert.False(string.IsNullOrEmpty(generated), "Expected a generated file to be produced");
            return File.ReadAllBytes(generated);
        }

        [Fact]
        public void NullTemplateProducesNoFile()
        {
            using var sut = new DefaultRdpUrlParser(new ParserConfig { ParserId = "rdp" });
            var dictionary = sut.Parse(RdpUrl);
            Assert.Equal(string.Empty, dictionary[Token.GeneratedFile]);
        }

        [Fact]
        public void InlineRdpTemplateWritesCrlfAndUtf16Bom()
        {
            var config = new ParserConfig
            {
                ParserId = "rdp",
                TemplateContent = "full address:s:%Host%:%Port%\nusername:s:%User%",
                TemplateExtension = ".rdp",
            };

            var bytes = GenerateBytes(config);

            // UTF-16 LE BOM
            Assert.True(bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE, "Expected UTF-16 LE BOM for .rdp");

            var text = new UnicodeEncoding(false, true).GetString(bytes);
            Assert.Contains("full address:s:myhost:3389", text);
            Assert.Contains("\r\n", text);
            Assert.DoesNotContain("\n\n", text); // no bare/doubled LF artifacts
        }

        [Fact]
        public void InlineNonRdpTemplateWritesLfAndUtf8NoBom()
        {
            var config = new ParserConfig
            {
                ParserId = "rdp",
                TemplateContent = "[connection]\nname=%Host%\nserver=%Host%",
                TemplateExtension = ".remmina",
            };

            var bytes = GenerateBytes(config);

            // No BOM of any kind
            Assert.False(bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE, "Did not expect UTF-16 BOM");
            Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF, "Did not expect UTF-8 BOM");

            var text = Encoding.UTF8.GetString(bytes);
            Assert.Contains("server=myhost", text);
            Assert.Contains("\n", text);
            Assert.DoesNotContain("\r\n", text);
        }

        [Fact]
        public void ExplicitOverridesWinOverDefaults()
        {
            // Force LF + UTF-8 on a .rdp template
            var config = new ParserConfig
            {
                ParserId = "rdp",
                TemplateContent = "full address:s:%Host%\nusername:s:%User%",
                TemplateExtension = ".rdp",
                LineEnding = TemplateLineEnding.Lf,
                Encoding = TemplateEncoding.Utf8,
            };

            var bytes = GenerateBytes(config);

            Assert.False(bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE, "Override to Utf8 should drop the UTF-16 BOM");
            var text = Encoding.UTF8.GetString(bytes);
            Assert.DoesNotContain("\r\n", text);
        }

        [Fact]
        public void MigrationInlinesTemplateFileContent()
        {
            var template = Path.GetTempFileName();
            var remmina = Path.ChangeExtension(template, ".remmina");
            File.WriteAllText(remmina, "[connection]\r\nname=test\r\nserver=%Host%\r\n");
            try
            {
                var config = new ScalusConfig
                {
                    Applications = new List<ApplicationConfig>
                    {
                        new ApplicationConfig
                        {
                            Id = "app1",
                            Name = "app1",
                            Protocol = "rdp",
                            Parser = new ParserConfig { ParserId = "rdp", UseTemplateFile = remmina },
                        },
                    },
                };

                var changed = ScalusConfigurationBase.MigrateLegacyTemplates(config);
                Assert.True(changed);

                var parser = config.Applications.Single().Parser;
                Assert.True(parser.HasTemplate);
                Assert.Null(parser.UseTemplateFile);
                Assert.Equal(".remmina", parser.TemplateExtension);
                Assert.Contains("server=%Host%", parser.TemplateContent);
                // Stored LF-canonical
                Assert.DoesNotContain("\r", parser.TemplateContent);
            }
            finally
            {
                if (File.Exists(remmina))
                {
                    File.Delete(remmina);
                }

                if (File.Exists(template))
                {
                    File.Delete(template);
                }
            }
        }

        [Fact]
        public void MigrationInlinesDefaultRdpTemplate()
        {
            var config = new ScalusConfig
            {
                Applications = new List<ApplicationConfig>
                {
                    new ApplicationConfig
                    {
                        Id = "app1",
                        Name = "app1",
                        Protocol = "rdp",
                        Parser = new ParserConfig { ParserId = "rdp", UseDefaultTemplate = true },
                    },
                },
            };

            var changed = ScalusConfigurationBase.MigrateLegacyTemplates(config);
            Assert.True(changed);

            var parser = config.Applications.Single().Parser;
            Assert.True(parser.HasTemplate);
            Assert.False(parser.UseDefaultTemplate);
            Assert.Equal(".rdp", parser.TemplateExtension);
            Assert.NotEmpty(parser.TemplateContent);
        }

        [Fact]
        public void RepairRestoresMissingTemplateFromSeedByMatchingId()
        {
            var config = new ScalusConfig
            {
                Applications = new List<ApplicationConfig>
                {
                    new ApplicationConfig
                    {
                        Id = "WindowsRDPDesktopOrApp",
                        Name = "WindowsRDPDesktopOrApp",
                        Protocol = "rdp",
                        Exec = "mstsc.exe",
                        Args = new List<string> { "%GeneratedFile%" },
                        Parser = new ParserConfig { ParserId = "rdp" },
                    },
                },
            };

            var seedApps = new List<ApplicationConfig>
            {
                new ApplicationConfig
                {
                    Id = "windows-rdp",
                    Parser = new ParserConfig { ParserId = "rdp", TemplateContent = "GENERIC-1841", TemplateExtension = ".rdp" },
                },
                new ApplicationConfig
                {
                    Id = "WindowsRDPDesktopOrApp",
                    Parser = new ParserConfig { ParserId = "rdp", TemplateContent = "DESKTOP-OR-APP-1071", TemplateExtension = ".rdp" },
                },
            };

            var changed = ScalusConfigurationBase.RepairMissingTemplatesFromSeed(config, seedApps);

            Assert.True(changed);
            var parser = config.Applications.Single().Parser;
            Assert.True(parser.HasTemplate);
            // The template must come from the seed app with the SAME Id, not the first rdp seed.
            Assert.Equal("DESKTOP-OR-APP-1071", parser.TemplateContent);
            Assert.Equal(".rdp", parser.TemplateExtension);
        }

        [Fact]
        public void RepairRestoresFromPostProcessingGeneratedFile()
        {
            var config = new ScalusConfig
            {
                Applications = new List<ApplicationConfig>
                {
                    new ApplicationConfig
                    {
                        Id = "signed-rdp",
                        Protocol = "rdp",
                        Exec = "mstsc.exe",
                        Args = new List<string> { "/v:%Host%" },
                        Parser = new ParserConfig
                        {
                            ParserId = "rdp",
                            PostProcessingArgs = new List<string> { "/sha256", "%Thumbprint%", "%GeneratedFile%" },
                        },
                    },
                },
            };

            var seedApps = new List<ApplicationConfig>
            {
                new ApplicationConfig
                {
                    Id = "signed-rdp",
                    Parser = new ParserConfig { ParserId = "rdp", TemplateContent = "SIGNED-TEMPLATE", TemplateExtension = ".rdp" },
                },
            };

            Assert.True(ScalusConfigurationBase.RepairMissingTemplatesFromSeed(config, seedApps));
            Assert.Equal("SIGNED-TEMPLATE", config.Applications.Single().Parser.TemplateContent);
        }

        [Fact]
        public void RepairLeavesCommandLineOnlyAppUntouched()
        {
            // freerdp builds its command line from tokens and never references %GeneratedFile%,
            // so it legitimately has no template and must not be "repaired".
            var config = new ScalusConfig
            {
                Applications = new List<ApplicationConfig>
                {
                    new ApplicationConfig
                    {
                        Id = "freerdp",
                        Protocol = "rdp",
                        Exec = "/usr/bin/xfreerdp",
                        Args = new List<string> { "/u:%User%", "/v:%Host%:%Port%", "/p:Safeguard" },
                        Parser = new ParserConfig { ParserId = "rdp" },
                    },
                },
            };

            var seedApps = new List<ApplicationConfig>
            {
                new ApplicationConfig
                {
                    Id = "freerdp",
                    Parser = new ParserConfig { ParserId = "rdp", TemplateContent = "SHOULD-NOT-BE-USED" },
                },
            };

            Assert.False(ScalusConfigurationBase.RepairMissingTemplatesFromSeed(config, seedApps));
            Assert.False(config.Applications.Single().Parser.HasTemplate);
        }

        [Fact]
        public void RepairIsNoOpWhenNoSeedIdMatches()
        {
            var config = new ScalusConfig
            {
                Applications = new List<ApplicationConfig>
                {
                    new ApplicationConfig
                    {
                        Id = "custom-rdp",
                        Protocol = "rdp",
                        Args = new List<string> { "%GeneratedFile%" },
                        Parser = new ParserConfig { ParserId = "rdp" },
                    },
                },
            };

            var seedApps = new List<ApplicationConfig>
            {
                new ApplicationConfig
                {
                    Id = "windows-rdp",
                    Parser = new ParserConfig { ParserId = "rdp", TemplateContent = "GENERIC" },
                },
            };

            Assert.False(ScalusConfigurationBase.RepairMissingTemplatesFromSeed(config, seedApps));
            Assert.False(config.Applications.Single().Parser.HasTemplate);
        }

        [Fact]
        public void RepairDoesNotOverwriteAppThatAlreadyHasTemplate()
        {
            var config = new ScalusConfig
            {
                Applications = new List<ApplicationConfig>
                {
                    new ApplicationConfig
                    {
                        Id = "windows-rdp",
                        Protocol = "rdp",
                        Args = new List<string> { "%GeneratedFile%" },
                        Parser = new ParserConfig { ParserId = "rdp", TemplateContent = "USER-EDITED" },
                    },
                },
            };

            var seedApps = new List<ApplicationConfig>
            {
                new ApplicationConfig
                {
                    Id = "windows-rdp",
                    Parser = new ParserConfig { ParserId = "rdp", TemplateContent = "SEED" },
                },
            };

            Assert.False(ScalusConfigurationBase.RepairMissingTemplatesFromSeed(config, seedApps));
            Assert.Equal("USER-EDITED", config.Applications.Single().Parser.TemplateContent);
        }

        [Fact]
        public void MigrationIsNoOpWhenAlreadyInline()
        {
            var config = new ScalusConfig
            {
                Applications = new List<ApplicationConfig>
                {
                    new ApplicationConfig
                    {
                        Id = "app1",
                        Name = "app1",
                        Protocol = "rdp",
                        Parser = new ParserConfig { ParserId = "rdp", TemplateContent = "full address:s:%Host%" },
                    },
                },
            };

            Assert.False(ScalusConfigurationBase.MigrateLegacyTemplates(config));
        }
    }
}
