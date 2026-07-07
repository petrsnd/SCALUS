// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Application.cs" company="One Identity Inc.">
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

namespace OneIdentity.Scalus.Launch
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using OneIdentity.Scalus.Dto;
    using OneIdentity.Scalus.Platform;
    using OneIdentity.Scalus.Util;
    using Serilog.Context;

    internal class Application : IApplication
    {
        public Application(Launch.Options options, IOsServices osServices, IScalusConfiguration config)
        {
            this.Options = options;
            Config = config;
            OsServices = osServices;
        }

        private Launch.Options Options { get; }

        private IScalusConfiguration Config { get; }

        private IOsServices OsServices { get; }

        public int Run()
        {
            // A short correlation id shared by every log line and the persisted record for this launch.
            var launchId = Guid.NewGuid().ToString("N").Substring(0, 12);
            var record = new LaunchRecord
            {
                LaunchId = launchId,
                TimestampUtc = DateTime.UtcNow,
                Protocol = GetProtocol(Options.Url),
                Url = SensitiveData.Redact(Options.Url),
                LauncherBinary = CurrentBinaryName(),
            };
            var stopwatch = Stopwatch.StartNew();

            // The trailing space keeps the log template tidy ("[abc] message") and renders as nothing
            // when the property is absent on non-launch log lines.
            using (LogContext.PushProperty("LaunchId", $"[{launchId}] "))
            {
                Serilog.Log.Debug($"Dispatching URL: {SensitiveData.Redact(Options.Url)}");

                try
                {
                    using var handler = Config.GetProtocolHandler(Options.Url);
                    if (handler == null)
                    {
                        // We are the registered application, but we can't parse the config
                        // or nothing is configured, show an error somehow
                        var msg = $"SCALUS configuration does not provide a method to handle the URL: {SensitiveData.Redact(Options.Url)}";
                        OsServices.OpenText(msg);
                        record.Outcome = "config-error";
                        record.Error = msg;
                        return Finish(record, stopwatch, 1);
                    }

                    var result = handler.Run(Options.Preview);
                    record.ApplicationId = result.ApplicationId;
                    record.Command = result.Command;
                    record.Outcome = LaunchResult.OutcomeKeyword(result.Outcome);
                    record.ExitCode = result.ExitCode;
                    record.Error = result.Error;
                    return Finish(record, stopwatch, result.Success ? 0 : 1);
                }
                catch (Exception ex)
                {
                    HandleLaunchError(ex, Options.Url);
                    record.Outcome = "error";
                    record.Error = SensitiveData.Redact(ex.Message);
                    return Finish(record, stopwatch, 1);
                }
            }
        }

        private static string GetProtocol(string url)
        {
            var protocolSeparatorIndex = url.IndexOf("://");
            if (protocolSeparatorIndex == -1)
            {
                return string.Empty;
            }

            return url.Substring(0, protocolSeparatorIndex);
        }

        private static string CurrentBinaryName()
        {
            try
            {
                return Path.GetFileNameWithoutExtension(Environment.ProcessPath);
            }
            catch
            {
                return null;
            }
        }

        private static ApplicationConfig GetApplicationForProtocol(ScalusConfig config, string protocol)
        {
            var application = config.Protocols.FirstOrDefault(x => x.Protocol == protocol);
            if (application == null)
            {
                return null;
            }

            return config.Applications.FirstOrDefault(x => x.Id == application.AppId);
        }

        private static int Finish(LaunchRecord record, Stopwatch stopwatch, int exitCode)
        {
            stopwatch.Stop();
            record.DurationMs = stopwatch.ElapsedMilliseconds;
            record.Success = exitCode == 0;
            LaunchRecordStore.Write(record);
            return exitCode;
        }

        private void HandleLaunchError(Exception ex, string url)
        {
            ApplicationConfig application = null;
            var scalusJsonPath = ConfigurationManager.ScalusJson;

            try
            {
                application = GetApplicationForProtocol(Config.GetConfiguration(), GetProtocol(url));
            }
            catch (Exception e)
            {
                OsServices.ShowMessage($"Failed to read config file:{scalusJsonPath}:{e.Message}");
            }

            var msg =
$@"[SCALUS]: Failed to launch registered URL handler.
  URL:           {SensitiveData.Redact(url)}
  Error:         {SensitiveData.Redact(ex.Message)}

  ApplicationId: {application?.Id ?? "<none>"}
  Command:       {application?.Exec ?? "<none>"}
  Args:          {(application?.Args != null ? SensitiveData.Redact(string.Join(" ", application?.Args)) : "<none>")}

  Config File:   {scalusJsonPath}  

Check the configuration for this URL protocol.";
            OsServices.OpenText(msg);
            Serilog.Log.Error(ex, msg);
        }
    }
}
