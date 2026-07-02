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
