// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ScalusConfigurationBase.cs" company="One Identity Inc.">
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

namespace OneIdentity.Scalus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.Json;
    using OneIdentity.Scalus.Dto;
    using OneIdentity.Scalus.Util;

    internal class ScalusConfigurationBase
    {
        private ScalusConfig scalusConfig;

        protected ScalusConfigurationBase()
        {
        }

        public List<string> ValidationErrors { get; protected set; } = new List<string>();

        protected string configFile { get; } = ConfigurationManager.ScalusJson;

        protected ScalusConfig Config
        {
            get
            {
                if (scalusConfig == null)
                {
                    scalusConfig = Load(configFile);
                }

                return scalusConfig;
            }

            set
            {
                scalusConfig = value;
            }
        }

        public ScalusServerConfig GetConfiguration()
        {
            var serverConfig = new ScalusServerConfig
            {
                Applications = Config.Applications,
                Protocols = Config.Protocols,
                Edition = Edition.Supported,
            };

#if COMMUNITY_EDITION
            serverConfig.Edition = Edition.Community;
#endif
            return serverConfig;
        }

        public (bool, ScalusConfig) Validate(string json, bool strict = false)
        {
            var config = new ScalusConfig();
            ValidationErrors = new List<string>();
            try
            {
                config = ScalusJson.Deserialize(json, strict);
            }
            catch (Exception e)
            {
                ValidationErrors.Add($"Error deserialising json configuration:{e.Message}");
            }

            try
            {
                config?.Validate(ValidationErrors, false);
            }
            catch (Exception e)
            {
                ValidationErrors.Add($"Error validating json configuration:{e.Message}");
                return (false, config);
            }

            return (ValidationErrors.Count == 0, config);
        }

        // One-time upgrade: fold legacy template mechanisms (UseDefaultTemplate / UseTemplateFile) into
        // inline TemplateContent so the template is visible and travels with the configuration. Runs in
        // memory on every load; once the config is re-saved the legacy fields disappear. Returns true if
        // anything was changed so callers can choose to persist.
        internal static bool MigrateLegacyTemplates(ScalusConfig config)
        {
            if (config?.Applications == null)
            {
                return false;
            }

            var changed = false;
            foreach (var app in config.Applications)
            {
                var parser = app?.Parser;
                if (parser == null || parser.HasTemplate)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(parser.UseTemplateFile))
                {
                    var resolved = ResolveConfigTokens(parser.UseTemplateFile);
                    if (File.Exists(resolved))
                    {
                        try
                        {
                            var text = File.ReadAllText(resolved);
                            parser.TemplateContent = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal);
                            var ext = Path.GetExtension(resolved);
                            if (!string.IsNullOrEmpty(ext))
                            {
                                parser.TemplateExtension = ext;
                            }

                            parser.UseTemplateFile = null;
                            parser.UseDefaultTemplate = false;
                            changed = true;
                        }
                        catch (Exception e)
                        {
                            Serilog.Log.Warning($"Could not migrate template file '{resolved}' for application '{app.Id}': {e.Message}. Leaving UseTemplateFile in place.");
                        }
                    }
                    else
                    {
                        Serilog.Log.Warning($"Template file '{resolved}' referenced by application '{app.Id}' does not exist; leaving UseTemplateFile in place for review.");
                    }
                }
                else if (parser.UseDefaultTemplate)
                {
                    parser.TemplateContent = UrlParser.DefaultRdpUrlParser.GetDefaultTemplateText();
                    parser.TemplateExtension = ".rdp";
                    parser.UseDefaultTemplate = false;
                    changed = true;
                }
            }

            return changed;
        }

        protected ScalusConfig Load(string path)
        {
            ValidationErrors = new List<string>();
            var config = new ScalusConfig();

            if (!File.Exists(path))
            {
                ValidationErrors.Add($"Missing config file:{path}");
            }
            else
            {
                var configJson = File.ReadAllText(path);
                (_, config) = Validate(configJson);
            }

            if (ValidationErrors.Count > 0)
            {
                Serilog.Log.Error($"**** Validation of {configFile} failed");
                Serilog.Log.Error($"*** Validation errors: {string.Join(", ", ValidationErrors)}");
            }

            MigrateLegacyTemplates(config);
            return config;
        }

        private static string ResolveConfigTokens(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return value
                .Replace("%AppData%", ConfigurationManager.ProdAppPath, StringComparison.OrdinalIgnoreCase)
                .Replace("%Home%", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), StringComparison.OrdinalIgnoreCase)
                .Replace("%TempPath%", Path.GetTempPath(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
