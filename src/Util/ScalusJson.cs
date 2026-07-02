// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ScalusJson.cs" company="One Identity Inc.">
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

namespace OneIdentity.Scalus.Util
{
    using System.Text.Encodings.Web;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using OneIdentity.Scalus.Dto;
    using OneIdentity.Scalus.Verify;

    // Source-generated serialization metadata for the configuration model. Using a context keeps
    // the (de)serialization reflection-free so Scalus.Core stays trim/NativeAOT friendly. The disk
    // format is camelCase and indented; enums are written as their names (see the per-enum
    // JsonStringEnumConverter attributes) so the file stays human-readable.
    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        WriteIndented = true)]
    [JsonSerializable(typeof(ScalusConfig))]
    [JsonSerializable(typeof(ScalusServerConfig))]
    [JsonSerializable(typeof(VerifyResult))]
    internal partial class ScalusJsonContext : JsonSerializerContext
    {
    }

    // Shared System.Text.Json options for reading and writing SCALUS configuration on disk.
    internal static class ScalusJson
    {
        // Read/write the configuration file. camelCase + indented (baked into the source-gen
        // context), case-insensitive on read so legacy/hand-edited PascalCase files still load, and
        // the relaxed encoder so inline template content is written with minimal escaping.
        public static readonly JsonSerializerOptions Disk = Build(strict: false);

        // Same as Disk but rejects unknown members, used by the strict validation path.
        public static readonly JsonSerializerOptions DiskStrict = Build(strict: true);

        // Serialize using the value's runtime type so a ScalusServerConfig (which adds Edition) is
        // written whole, matching the previous Newtonsoft behaviour.
        public static string Serialize(ScalusConfig configuration) =>
            JsonSerializer.Serialize(configuration, configuration.GetType(), Disk);

        public static ScalusConfig Deserialize(string json, bool strict = false) =>
            JsonSerializer.Deserialize<ScalusConfig>(json, strict ? DiskStrict : Disk);

        private static JsonSerializerOptions Build(bool strict)
        {
            var options = new JsonSerializerOptions(ScalusJsonContext.Default.Options)
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            };

            if (strict)
            {
                options.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
            }

            return options;
        }
    }
}
