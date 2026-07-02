// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SensitiveData.cs" company="One Identity Inc.">
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
    using System.Text.RegularExpressions;

    /// <summary>
    /// Masks secrets before they reach the log. The Safeguard/SPS in-band username carries a
    /// one-time authorization token; it must never be written to disk in clear text.
    /// </summary>
    internal static class SensitiveData
    {
        // Matches the in-band one-time token in either separator style (token=... for ssh,
        // token~... for rdp) and stops at the next field separator.
        private static readonly Regex TokenPattern = new Regex(
            "(?i)(token\\s*[=~]\\s*)([^@%&\\s\"']+)",
            RegexOptions.Compiled);

        public static string Redact(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return TokenPattern.Replace(value, "$1***");
        }
    }
}
