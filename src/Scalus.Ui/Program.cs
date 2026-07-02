// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="One Identity Inc.">
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
    using System.IO;
    using Photino.NET;
    using Serilog;

    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File(
                    Path.Combine(Path.GetTempPath(), "scalus-ui.log"),
                    rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "SCALUS configuration app terminated unexpectedly");
                throw;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static void Run()
        {
            var baseDir = AppContext.BaseDirectory;
            var indexPath = Path.Combine(baseDir, "wwwroot", "index.html");
            var iconPath = Path.Combine(baseDir, "scalus.ico");
            var container = Ioc.RegisterApplication(Log.Logger);
            var dispatcher = new BridgeDispatcher(container);

            Log.Information("Starting SCALUS configuration app ({IndexPath})", indexPath);

            var window = new PhotinoWindow()
                .SetTitle("SCALUS")
                .SetUseOsDefaultSize(false)
                .SetSize(1240, 840)
                .SetMinSize(960, 640)
                .Center()
                .SetContextMenuEnabled(false)
                .SetDevToolsEnabled(true);

            if (File.Exists(iconPath))
            {
                window.SetIconFile(iconPath);
            }

            dispatcher.Attach(window);
            window.RegisterWebMessageReceivedHandler((sender, message) =>
            {
                var self = (PhotinoWindow)sender;
                var response = dispatcher.Dispatch(message);
                self.SendWebMessage(response);
            });

            window.Load(indexPath);
            window.WaitForClose();
        }
    }
}
