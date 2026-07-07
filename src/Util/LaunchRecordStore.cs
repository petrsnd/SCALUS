// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LaunchRecordStore.cs" company="One Identity Inc.">
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
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using OneIdentity.Scalus.Dto;
    using Serilog;

    // Persists per-launch records as one JSON file per launch under ConfigurationManager.LaunchRecordsDir.
    // One file per launch avoids cross-process append locking between concurrent one-shot launchers,
    // and lets the configuration UI aggregate the newest records for its Recent launches view.
    internal static class LaunchRecordStore
    {
        // Keep a bounded window of recent launches; older files are pruned on each write.
        public const int RetainedRecordLimit = 200;

        private const string FileExtension = ".json";

        // Writes one record file, named {baseName}.json so it sits alongside the launch's generated
        // file ({baseName}{ext}) written by the parser into the same directory. Never throws: a
        // logging failure must not break the actual launch.
        public static string Write(LaunchRecord record, string baseName = null)
        {
            try
            {
                var dir = ConfigurationManager.LaunchRecordsDir;
                baseName = string.IsNullOrEmpty(baseName)
                    ? $"{record.TimestampUtc:yyyyMMddTHHmmssfffZ}-{Sanitize(record.LaunchId)}"
                    : baseName;
                var path = Path.Combine(dir, baseName + FileExtension);
                File.WriteAllText(path, ScalusJson.Serialize(record));
                Prune(dir);
                return path;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to write launch record");
                return null;
            }
        }

        // Returns the persisted records newest-first (best effort; skips any unreadable/partial files).
        public static IReadOnlyList<LaunchRecord> List(int max = RetainedRecordLimit)
        {
            var results = new List<LaunchRecord>();
            try
            {
                var dir = ConfigurationManager.LaunchRecordsDir;
                foreach (var file in EnumerateNewestFirst(dir).Take(max))
                {
                    try
                    {
                        results.Add(ScalusJson.DeserializeLaunchRecord(File.ReadAllText(file)));
                    }
                    catch (Exception ex)
                    {
                        Log.Debug(ex, "Skipping unreadable launch record {File}", file);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Failed to list launch records");
            }

            return results;
        }

        private static void Prune(string dir)
        {
            try
            {
                // Each retained launch is a group of files sharing a base name: the {base}.json record
                // plus any {base}{ext} generated file the parser persisted. Prune whole groups so a
                // record and its generated file are always removed together.
                var stale = EnumerateNewestFirst(dir).Skip(RetainedRecordLimit).ToList();
                foreach (var file in stale)
                {
                    var baseName = Path.GetFileNameWithoutExtension(file);
                    foreach (var groupFile in Directory.EnumerateFiles(dir, baseName + ".*"))
                    {
                        try
                        {
                            File.Delete(groupFile);
                        }
                        catch (Exception ex)
                        {
                            Log.Debug(ex, "Failed to prune launch record {File}", groupFile);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Failed to prune launch records");
            }
        }

        // Files are named with a sortable UTC timestamp prefix, so lexical descending order is also
        // newest-first without stat-ing each file.
        private static IEnumerable<string> EnumerateNewestFirst(string dir) =>
            Directory.Exists(dir)
                ? Directory.EnumerateFiles(dir, "*" + FileExtension).OrderByDescending(f => f, StringComparer.Ordinal)
                : Enumerable.Empty<string>();

        private static string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "unknown";
            }

            foreach (var c in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(c, '_');
            }

            return value;
        }
    }
}
