// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WindowsBasicProtocolRegistrar.cs" company="One Identity Inc.">
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
    using System.Runtime.Versioning;
    using System.Text;
    using OneIdentity.Scalus.Platform;
    using OneIdentity.Scalus.Util;

    [SupportedOSPlatform("windows")]
    internal class WindowsBasicProtocolRegistrar : IProtocolRegistrar
    {
        public IOsServices OsServices { get; }

        public bool UseSudo { get; set; }

        public bool RootMode { get; set; }

        public string Name { get; } = "WindowsProtocolRegistry";

        public bool IsScalusRegistered(string protocol)
        {
            var command = GetRegisteredCommand(protocol);

            if (string.IsNullOrEmpty(command))
            {
                return false;
            }

            return CommandInvokesThisBinary(command);
        }

        public string GetRegisteredCommand(string protocol)
        {
            var path = GetPathRoot(protocol);

            if (!RegistryUtils.PathExists(path))
            {
                return null;
            }

            var commandPath = path + @"\shell\open\command";
            if (!RegistryUtils.PathExists(commandPath))
            {
                return null;
            }

            return RegistryUtils.GetStringValue(commandPath, string.Empty);
        }

        public bool Register(string protocol)
        {
            var path = GetPathRoot(protocol);
            var registrationCommand = Constants.GetLaunchCommand("\"%1\"");
            Serilog.Log.Debug($"Registering to run {registrationCommand} for {protocol} URLs.");

            if (RegistryUtils.SetValue(path, string.Empty, $"SCALUS {protocol} Handler") &&
                RegistryUtils.SetValue(path, "URL Protocol", string.Empty) &&
                RegistryUtils.SetValue(path + "\\DefaultIcon", string.Empty, "%systemroot%\\system32\\mstsc.exe") &&
                RegistryUtils.SetValue(path + "\\shell\\open\\command", string.Empty, registrationCommand))
            {
                return true;
            }

            return false;
        }

        public bool Unregister(string protocol)
        {
            var path = GetPathRoot(protocol);
            if (RegistryUtils.PathExists(path) && !RegistryUtils.DeleteKey(path))
            {
                return false;
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

        private static string GetPathRoot(string protocol)
        {
            return $"HKEY_CURRENT_USER\\SOFTWARE\\Classes\\{protocol}";
        }

        // The registered handler command embeds the full path to the scalus binary (see
        // Constants.GetLaunchCommand). Confirm one of its tokens resolves to *this* binary
        // rather than matching the bare file name, which would also match a different
        // scalus install or an unrelated handler that merely mentions the name.
        private static bool CommandInvokesThisBinary(string command)
        {
            string ourBinary;
            try
            {
                ourBinary = Path.GetFullPath(Constants.GetBinaryPath());
            }
            catch (Exception)
            {
                return false;
            }

            foreach (var token in SplitCommandLine(command))
            {
                string full;
                try
                {
                    full = Path.GetFullPath(token);
                }
                catch (Exception)
                {
                    continue;
                }

                if (string.Equals(full, ourBinary, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<string> SplitCommandLine(string command)
        {
            var current = new StringBuilder();
            var inQuotes = false;

            foreach (var ch in command)
            {
                if (ch == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (char.IsWhiteSpace(ch) && !inQuotes)
                {
                    if (current.Length > 0)
                    {
                        yield return current.ToString();
                        current.Clear();
                    }
                }
                else
                {
                    current.Append(ch);
                }
            }

            if (current.Length > 0)
            {
                yield return current.ToString();
            }
        }
    }
}
