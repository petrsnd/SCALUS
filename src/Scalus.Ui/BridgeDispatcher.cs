// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BridgeDispatcher.cs" company="One Identity Inc.">
//   This software is licensed under the Apache 2.0 open source license.
//   https://github.com/OneIdentity/SCALUS/blob/master/LICENSE
//
//   Copyright One Identity LLC.
//   ALL RIGHTS RESERVED.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace OneIdentity.Scalus.Ui
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using Autofac;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using OneIdentity.Scalus.Dto;
    using OneIdentity.Scalus.Util;
    using Photino.NET;

    /// <summary>
    /// Bridges front-end requests to the in-process SCALUS core. Messages arrive as
    /// { id, method, args[] } and responses are returned as { id, ok, result|error }.
    /// </summary>
    internal sealed class BridgeDispatcher
    {
        private static readonly string[] BuiltInProtocols = { "rdp", "ssh" };

        private readonly ILifetimeScope container;
        private readonly IRegistration registration;
        private PhotinoWindow window;

        public BridgeDispatcher(ILifetimeScope container)
        {
            this.container = container;
            this.registration = container.Resolve<IRegistration>();
            SeedDefaultConfiguration();
        }

        public void Attach(PhotinoWindow host) => this.window = host;

        public string Dispatch(string message)
        {
            string id = null;
            try
            {
                var request = JObject.Parse(message);
                id = (string)request["id"];
                var method = (string)request["method"];
                var args = request["args"] as JArray ?? new JArray();

                object result = method switch
                {
                    "getConfig" => GetConfig(),
                    "saveConfig" => SaveConfig(args[0].ToObject<ScalusConfig>()),
                    "validate" => Validate(args[0].ToObject<ScalusConfig>()),
                    "getRegistrations" => GetRegistrations(),
                    "register" => Register((string)args[0], (string)args[1]),
                    "unregister" => Unregister((string)args[0]),
                    "getTokens" => GetTokens(),
                    "getApplicationDescriptions" => GetApplicationDescriptions(),
                    "getParsers" => ProtocolHandlerFactory.GetSupportedParsers(),
                    "getInfo" => GetInfo(),
                    "exportToFile" => ExportToFile((string)args[0], (string)args[1]),
                    "importFromFile" => ImportFromFile(),
                    "getPlatform" => GetPlatform(),
                    _ => throw new InvalidOperationException($"Unknown method '{method}'."),
                };

                return JsonConvert.SerializeObject(new { id, ok = true, result });
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Bridge call failed");
                return JsonConvert.SerializeObject(new { id, ok = false, error = ex.Message });
            }
        }

        private ScalusServerConfig GetConfig() =>
            this.container.Resolve<IScalusApiConfiguration>().GetConfiguration();

        private object SaveConfig(ScalusConfig config)
        {
            var errors = this.container.Resolve<IScalusApiConfiguration>().SaveConfiguration(config);
            return new { errors };
        }

        private List<string> Validate(ScalusConfig config)
        {
            var errors = new List<string>();
            config?.Validate(errors, false);
            return errors;
        }

        private List<string> GetRegistrations()
        {
            var config = GetConfig();
            var schemes = BuiltInProtocols
                .Concat(config.Protocols?.Select(p => p.Protocol) ?? Enumerable.Empty<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase);

            return schemes.Where(s => this.registration.IsRegistered(s)).ToList();
        }

        private object Register(string protocol, string scope)
        {
            var rootMode = string.Equals(scope, "all", StringComparison.OrdinalIgnoreCase);
            if (!this.registration.Register(new[] { protocol }, force: true, rootMode: rootMode, useSudo: false))
            {
                throw new InvalidOperationException($"Failed to register '{protocol}'.");
            }

            return null;
        }

        private object Unregister(string protocol)
        {
            this.registration.UnRegister(new[] { protocol }, rootMode: false, useSudo: false);
            return null;
        }

        private Dictionary<string, string> GetTokens() =>
            ParserConfigDefinitions.TokenDescription.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value);

        private Dictionary<string, string> GetApplicationDescriptions() =>
            ScalusConfig.DtoPropertyDescription;

        private string GetInfo()
        {
            var config = GetConfig();
            var registered = GetRegistrations();
            var lines = new List<string>
            {
                $"Platform: {GetPlatform()}",
                $"Edition: {config.Edition}",
                $"Configuration file: {ConfigurationManager.ScalusJson}",
                $"Applications defined: {config.Applications?.Count ?? 0}",
                $"Protocols configured: {config.Protocols?.Count ?? 0}",
                $"Registered handlers: {(registered.Count == 0 ? "none" : string.Join(", ", registered))}",
            };
            return string.Join(Environment.NewLine, lines);
        }

        private bool ExportToFile(string defaultName, string contents)
        {
            if (this.window == null)
            {
                return false;
            }

            // Photino's native save dialog skips display entirely when defaultPath does not
            // resolve to an existing item, so open in an existing folder rather than passing a
            // suggested (non-existent) file name.
            var target = this.window.ShowSaveFile(
                title: "Export configuration",
                defaultPath: Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                filters: new (string, string[])[] { ("JSON", new[] { "json" }), ("All files", new[] { "*" }) });

            if (string.IsNullOrEmpty(target))
            {
                return false;
            }

            // The native dialog does not enforce a default extension, so add one when the
            // chosen name has none.
            if (string.IsNullOrEmpty(Path.GetExtension(target)))
            {
                target = Path.ChangeExtension(target, "json");
            }

            File.WriteAllText(target, contents);
            return true;
        }

        private string ImportFromFile()
        {
            if (this.window == null)
            {
                return null;
            }

            var selected = this.window.ShowOpenFile(
                title: "Import configuration",
                multiSelect: false,
                filters: new (string, string[])[] { ("JSON", new[] { "json" }), ("All files", new[] { "*" }) });

            var path = selected?.FirstOrDefault();
            return string.IsNullOrEmpty(path) ? null : File.ReadAllText(path);
        }

        private static string GetPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "Windows";
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "Mac";
            }

            return "Linux";
        }

        private void SeedDefaultConfiguration()
        {
            try
            {
                var api = this.container.Resolve<IScalusApiConfiguration>();
                var config = api.GetConfiguration();
                var changed = false;

                // First run (or a previously emptied config): preload the shipped
                // applications so the Applications screen isn't blank, and present the
                // built-in protocols as unconfigured so nothing is registered without
                // the user explicitly choosing an application.
                if (config.Applications == null || config.Applications.Count == 0)
                {
                    var seedApps = LoadSeedApplications();
                    if (seedApps.Count > 0)
                    {
                        config.Applications = seedApps;
                        config.Protocols = BuiltInProtocols
                            .Select(p => new ProtocolMapping { Protocol = p, AppId = string.Empty })
                            .ToList();
                        changed = true;
                        Serilog.Log.Information(
                            "Seeded {Count} default applications with unconfigured built-in protocols",
                            seedApps.Count);
                    }
                }

                // The built-in protocols can never be deleted, so make sure a row exists
                // for each one even in a hand-edited or imported configuration.
                if (EnsureBuiltInProtocols(config))
                {
                    changed = true;
                }

                if (changed)
                {
                    api.SaveConfiguration(config);
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "Failed to seed the default configuration");
            }
        }

        private static bool EnsureBuiltInProtocols(ScalusConfig config)
        {
            config.Protocols ??= new List<ProtocolMapping>();
            var changed = false;
            foreach (var scheme in BuiltInProtocols)
            {
                if (!config.Protocols.Any(p =>
                    string.Equals(p.Protocol, scheme, StringComparison.OrdinalIgnoreCase)))
                {
                    config.Protocols.Add(new ProtocolMapping { Protocol = scheme, AppId = string.Empty });
                    changed = true;
                }
            }

            return changed;
        }

        private static List<ApplicationConfig> LoadSeedApplications()
        {
            var seedName = GetPlatform() switch
            {
                "Windows" => "windows.json",
                "Mac" => "mac.json",
                _ => "linux.json",
            };
            var seed = Path.Combine(AppContext.BaseDirectory, "defaults", seedName);
            if (!File.Exists(seed))
            {
                return new List<ApplicationConfig>();
            }

            try
            {
                var parsed = JsonConvert.DeserializeObject<ScalusConfig>(File.ReadAllText(seed));
                return parsed?.Applications ?? new List<ApplicationConfig>();
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "Failed to read seed applications from {Seed}", seed);
                return new List<ApplicationConfig>();
            }
        }
    }
}
