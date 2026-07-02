// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CommandLineRunner.cs" company="One Identity Inc.">
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
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading;
    using Microsoft.Extensions.DependencyInjection;
    using OneIdentity.Scalus.Util;
    using Serilog;
    using Serilog.Sinks.SystemConsole.Themes;

    /// <summary>
    /// Shared command-line pipeline used by both the CLI executable (scalus) and the
    /// desktop UI executable (scalus-ui) when it is invoked as a protocol handler
    /// (e.g. "scalus-ui launch -u rdp://..."). Both front-ends delegate here so that
    /// verb parsing, configuration bootstrap, and headless execution behave identically
    /// regardless of which binary the OS invoked to handle the URL.
    /// </summary>
    public static class CommandLineRunner
    {
        /// <summary>
        /// Parses the supplied command-line arguments, resolves the matching verb, and runs it
        /// headless. Returns the process exit code. Never opens a GUI window.
        /// </summary>
        public static int Run(string[] args)
        {
            bool community = false;
#if COMMUNITY_EDITION
            community = true;
#endif

            Console.WriteLine(community ? "Community Edition" : "Safeguard Edition");
            ConfigureLogging();
            CheckConfig();
            try
            {
                var logger = new LoggerConfiguration().WriteTo.Console(theme: ConsoleTheme.None).CreateLogger();
                using var provider = Ioc.RegisterApplication(logger);

                var parser = provider.GetRequiredService<ICommandLineParser>();
                var application = parser.Build(args, x => Ioc.CreateVerbApplication(provider, x), out var exitCode);

                // If application is null, then they ran help, version, or an invalid command
                if (application == null)
                {
                    ReleaseLaunchSemaphore();
                    return exitCode;
                }

                ReleaseLaunchSemaphore();
                return application.Run();
            }
            catch (Exception ex)
            {
                HandleUnexpectedError(ex);
            }

            return 1;
        }

        private static void ReleaseLaunchSemaphore()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            try
            {
                using var sem = Semaphore.OpenExisting("OneIdentity.Scalus");
                sem.Release();
            }
            catch (Exception)
            {
                Log.Debug("Failed to release semaphore!");

                // Failed to open the semaphore, so we can't signal it.
                // The launcher will time out after 15 seconds.
            }
        }

        private static void HandleUnexpectedError(Exception ex)
        {
            Log.Error($"Unexpected error: {ex.Message}", ex);
            string indent = "  ";
            while (ex.InnerException != null)
            {
                ex = ex.InnerException;
                Log.Error($"{indent}=> Inner Exception: {ex.Message}", ex);
                indent += "  ";
            }
        }

        private static void CheckConfig()
        {
            Log.Logger.Information($"CheckConfig");
            if (File.Exists(ConfigurationManager.ScalusJson))
            {
                Log.Logger.Information($"ok");
                try
                {
                    new ScalusApiConfiguration().MigrateOnDisk();
                }
                catch (Exception e)
                {
                    Log.Logger.Warning($"Template migration check failed: {e.Message}");
                }

                return;
            }

            var defpath = ConfigurationManager.ScalusJsonDefault;
            if (!File.Exists(defpath))
            {
                Log.Logger.Warning($"Config file not found:{ConfigurationManager.ScalusJson} and installed default file not found:{defpath}");
                return;
            }

            try
            {
                var dir = Path.GetDirectoryName(ConfigurationManager.ScalusJson);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                Log.Logger.Information($"Initializing config file:{ConfigurationManager.ScalusJson} from the installed file:{defpath}");
                File.WriteAllText(ConfigurationManager.ScalusJson, File.ReadAllText(defpath));

                var egs = ConfigurationManager.ExamplePath;
                if (Directory.Exists(egs))
                {
                    var files = Directory.EnumerateFiles(egs);
                    foreach (var one in files)
                    {
                        var to = Path.Combine(ConfigurationManager.ProdAppPath, Path.GetFileName(one));
                        if (File.Exists(one) && !File.Exists(to))
                        {
                            File.WriteAllText(to, File.ReadAllText(one));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Logger.Information($"Failed to initialize config file:{ConfigurationManager.ScalusJson} from installed file:{defpath}: {e.Message}");
            }
        }

        private static void ConfigureLogging()
        {
            var logFilePath = ConfigurationManager.LogFile;
            var config = new LoggerConfiguration();
            config.WriteTo.File(logFilePath, shared: true);
            if (ConfigurationManager.MinLogLevel != null)
            {
                config.MinimumLevel.ControlledBy(new Serilog.Core.LoggingLevelSwitch(ConfigurationManager.MinLogLevel.Value));
            }

            if (ConfigurationManager.LogToConsole)
            {
                config.WriteTo.Console();
            }

            Log.Logger = config.CreateLogger();
        }
    }
}
