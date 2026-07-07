// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Ioc.cs" company="One Identity Inc.">
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
    using System.Runtime.InteropServices;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using OneIdentity.Scalus.Platform;
    using OneIdentity.Scalus.Util;

    public static class Ioc
    {
        public static ServiceProvider RegisterApplication(Serilog.ILogger logger)
        {
            var services = new ServiceCollection();

            // Standard type registrations
            services.AddSingleton<Serilog.ILogger>(logger);
            services.AddSingleton<ICommandLineParser, CommandLineHandler>();
            services.AddSingleton<IRegistration, Registration>();
            services.AddSingleton<IScalusConfiguration, ScalusConfiguration>();
            services.AddTransient<IScalusApiConfiguration, ScalusApiConfiguration>();
            services.AddSingleton<IProtocolHandlerFactory, ProtocolHandlerFactory>();
            services.AddSingleton<ITerminalResolver, TerminalResolver>();

            // Perform platform-specific registrations here
            services.RegisterPlatformSpecificComponents();

            // Register the command-line verbs (both as IVerb for the parser and, via
            // VerbApplications below, mapped to the application that executes them).
            services.RegisterVerbs();

            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Builds the application that runs a parsed verb. The parsed options instance is passed
        /// straight into the application constructor (the rest of its dependencies come from the
        /// container), replacing Autofac's named resolution + TypedParameter.
        /// </summary>
        public static IApplication CreateVerbApplication(IServiceProvider services, object options)
        {
            var appType = options switch
            {
                Info.Options => typeof(Info.Application),
                Launch.Options => typeof(Launch.Application),
                Register.Options => typeof(Register.Application),
                Unregister.Options => typeof(Unregister.Application),
                Verify.Options => typeof(Verify.Application),
                _ => null,
            };

            return appType is null
                ? null
                : (IApplication)ActivatorUtilities.CreateInstance(services, appType, options);
        }

        private static void RegisterVerbs(this IServiceCollection services)
        {
            services.AddSingleton<IVerb, Info.Options>();
            services.AddSingleton<IVerb, Launch.Options>();
            services.AddSingleton<IVerb, Register.Options>();
            services.AddSingleton<IVerb, Unregister.Options>();
            services.AddSingleton<IVerb, Verify.Options>();
        }

        private static void RegisterPlatformSpecificComponents(this IServiceCollection services)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                services.RegisterWindowsComponents();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                services.RegisterLinuxComponents();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                services.RegisterOsxComponents();
            }
            else
            {
                // Register the "unsupported platform" component only if nothing else provides the
                // service (mirrors Autofac's PreserveExistingDefaults).
                services.TryAddSingleton<IProtocolRegistrar, UnsupportedPlatformRegistrar>();
            }

            services.TryAddSingleton<IUserInteraction, UserInteraction>();
            services.TryAddSingleton<IOsServices, OsServicesBase>();
        }

        private static void RegisterWindowsComponents(this IServiceCollection services)
        {
            if (OperatingSystem.IsWindows())
            {
                services.AddSingleton<IProtocolRegistrar, WindowsBasicProtocolRegistrar>();
                services.AddSingleton<IProtocolRegistrar, WindowsProtocolRegistrar>();
            }
        }

        private static void RegisterLinuxComponents(this IServiceCollection services)
        {
            services.AddSingleton<IProtocolRegistrar, UnixProtocolRegistrar>();
        }

        private static void RegisterOsxComponents(this IServiceCollection services)
        {
            //services.AddSingleton<IProtocolRegistrar, MacOSProtocolRegistrar>();
            services.AddSingleton<IProtocolRegistrar, MacOSUserDefaultRegistrar>();
        }
    }
}
