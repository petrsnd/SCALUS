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
    using System.Runtime.InteropServices;
    using Autofac;
    using Photino.NET;
    using Serilog;

    internal static class Program
    {
        private const string Scheme = "app";
        private const string StartUrl = "app://scalus/index.html";

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
            var wwwroot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
            var container = Ioc.RegisterApplication(Log.Logger);
            var dispatcher = new BridgeDispatcher(container);

            Log.Information("Starting SCALUS configuration app (wwwroot: {WwwRoot})", wwwroot);

            var window = new PhotinoWindow()
                .SetTitle("SCALUS")
                .SetUseOsDefaultSize(false)
                .SetSize(1240, 840)
                .SetMinSize(960, 640)
                .Center()
                .SetContextMenuEnabled(false)
                .SetDevToolsEnabled(true)
                .RegisterCustomSchemeHandler(Scheme, (object sender, string scheme, string url, out string contentType) =>
                    ServeAsset(wwwroot, url, out contentType));

            dispatcher.Attach(window);
            window.RegisterWebMessageReceivedHandler((sender, message) =>
            {
                var self = (PhotinoWindow)sender;
                var response = dispatcher.Dispatch(message);
                self.SendWebMessage(response);
            });

            window.Load(new Uri(StartUrl));
            window.WaitForClose();
        }

        private static Stream ServeAsset(string wwwroot, string url, out string contentType)
        {
            var path = "/index.html";
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                path = string.IsNullOrEmpty(uri.AbsolutePath) || uri.AbsolutePath == "/"
                    ? "/index.html"
                    : uri.AbsolutePath;
            }

            var relative = Uri.UnescapeDataString(path.TrimStart('/')).Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.GetFullPath(Path.Combine(wwwroot, relative));

            // Guard against path traversal outside the web root.
            if (!fullPath.StartsWith(Path.GetFullPath(wwwroot), StringComparison.OrdinalIgnoreCase) ||
                !File.Exists(fullPath))
            {
                fullPath = Path.Combine(wwwroot, "index.html");
            }

            contentType = ContentType(fullPath);
            return new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        private static string ContentType(string path) =>
            Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".html" => "text/html",
                ".js" => "text/javascript",
                ".mjs" => "text/javascript",
                ".css" => "text/css",
                ".json" => "application/json",
                ".ico" => "image/x-icon",
                ".svg" => "image/svg+xml",
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".woff" => "font/woff",
                ".woff2" => "font/woff2",
                ".ttf" => "font/ttf",
                ".map" => "application/json",
                _ => "application/octet-stream",
            };
    }
}
