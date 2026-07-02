// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WindowsProtocolRegistrar.cs" company="One Identity Inc.">
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
    using System.Linq;
    using System.Runtime.Versioning;
    using OneIdentity.Scalus.Platform;
    using OneIdentity.Scalus.Util;

    [SupportedOSPlatform("windows")]

    internal class WindowsProtocolRegistrar : IProtocolRegistrar
    {
        private static readonly string AppName = "SCALUS Protocol Handler";
        private static readonly string Clsid = "scalus.URLHandler.1";
        private static readonly string AppId = "SCALUS";
        private static readonly string AppCapabilitiesFragment = $"Software\\{AppId}\\Capabilities";
        private static readonly string CapabilitiesFragment = @"\Capabilities";
        private static readonly string CapabilitiesUrlAssociationsFragment = @$"{CapabilitiesFragment}\UrlAssociations";

        public IOsServices OsServices { get; }

        public bool UseSudo { get; set; }

        public bool RootMode { get; set; }

        public string Name { get; } = "WindowsURLRegistry";

        public bool IsScalusRegistered(string protocol)
        {
            // The URL association for this protocol must point at our ProgId ...
            if (RegistryUtils.GetStringValue(GetAppPath() + CapabilitiesUrlAssociationsFragment, protocol) != Clsid)
            {
                return false;
            }

            // ... and the ProgId's launch command must invoke *this* binary. A previous
            // install (for example the packaged build under Program Files) leaves the
            // association mapped to the same ProgId but with a command that launches a
            // different executable. Browsers such as Edge resolve the protocol through the
            // registered-application capability to that stale ProgId, so unless we confirm
            // the command targets the running binary we would silently hand launches to the
            // old install. Treat that as "not registered" so registration takes it over.
            var command = GetClsidCommand();
            return WindowsCommandLine.InvokesThisBinary(command);
        }

        public string GetRegisteredCommand(string protocol)
        {
            if (RegistryUtils.GetStringValue(GetAppPath() + CapabilitiesUrlAssociationsFragment, protocol) != Clsid)
            {
                return null;
            }

            return GetClsidCommand();
        }

        public bool Register(string protocol)
        {
            var registrationCommand = Constants.GetLaunchCommand("\"%1\"");
            Serilog.Log.Debug($"Registering to run {registrationCommand} for {protocol} URLs.");

            if (!RegisterClassId(registrationCommand))
            {
                Serilog.Log.Debug("Failed to register classid");
                return false;
            }

            if (!RegisterCapabilities(protocol))
            {
                Serilog.Log.Debug("Failed to register capabilities");
                return false;
            }

            if (!RegistryUtils.SetValue(GetRegisteredApplicationsPath(), AppName, AppCapabilitiesFragment))
            {
                Serilog.Log.Debug("Failed to register app capabilities");
                return false;
            }

            if (OperatingSystem.IsWindows())
            {
                if (!RegistryUtils.SetValue(GetAppAssociationToastsPath(), $"{Clsid}_{protocol}", 0, Microsoft.Win32.RegistryValueKind.DWord))
                {
                    Serilog.Log.Debug("Failed to register app association toasts");
                    return false;
                }
            }

            return true;
        }

        public bool Unregister(string protocol)
        {
            // Remove the protocol registration
            var urlAssociationsPath = GetAppPath() + CapabilitiesUrlAssociationsFragment;
            RegistryUtils.DeleteValue(urlAssociationsPath, protocol);

            // If there are more registrations, just return
            if (RegistryUtils.GetValueNames(urlAssociationsPath).Any())
            {
                return true;
            }

            RegistryUtils.DeleteValue(GetAppAssociationToastsPath(), $"{Clsid}_{protocol}");
            RegistryUtils.DeleteValue(GetRegisteredApplicationsPath(), AppName);

            foreach (var path in new[] { GetAppPath(), GetClassRegistrationPath() })
            {
                if (RegistryUtils.PathExists(path) && !RegistryUtils.DeleteKey(path))
                {
                    Serilog.Log.Debug($"Failed to remove registry path: {path}");
                    return false;
                }
            }

            return true;
        }

        public bool ReplaceRegistration(string protocol)
        {
            var res = Unregister(protocol);
            if (res)
            {
                res = Register(protocol);
            }

            return res;
        }

        private static bool RegisterClassId(string registrationCommand)
        {
            // Always (re)write the launch command so a stale command left by a previous
            // install is corrected. Returning early when the ProgId key already exists
            // would leave the old executable path in place and defeat re-registration.
            var path = GetClassRegistrationPath();
            return RegistryUtils.SetValue(path, string.Empty, AppName) &&
                   RegistryUtils.SetValue(path, "URL Protocol", string.Empty) &&
                   RegistryUtils.SetValue(path + "\\shell\\open\\command", string.Empty, registrationCommand);
        }

        private static string GetClsidCommand()
        {
            return RegistryUtils.GetStringValue(GetClassRegistrationPath() + "\\shell\\open\\command", string.Empty);
        }

        private static bool RegisterCapabilities(string protocol)
        {
            var path = GetAppPath();
            return RegistryUtils.SetValue(path + CapabilitiesFragment, "ApplicationDescription", AppName)
                && RegistryUtils.SetValue(path + CapabilitiesFragment, "ApplicationName", AppId)
                && RegistryUtils.SetValue(path + CapabilitiesUrlAssociationsFragment, protocol, Clsid);
        }

        private static string GetClassRegistrationPath()
        {
            return $"HKEY_CURRENT_USER\\SOFTWARE\\Classes\\{Clsid}";
        }

        private static string GetAppPath()
        {
            return $"HKEY_CURRENT_USER\\SOFTWARE\\{AppId}";
        }

        private static string GetRegisteredApplicationsPath()
        {
            return @"HKEY_CURRENT_USER\SOFTWARE\RegisteredApplications";
        }

        private static string GetAppAssociationToastsPath()
        {
            return @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\ApplicationAssociationToasts";
        }
    }
}
