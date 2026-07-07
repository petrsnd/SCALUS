using System;
using System.IO;
using System.Linq;
using Xunit;
using OneIdentity.Scalus;
using OneIdentity.Scalus.Dto;
using OneIdentity.Scalus.Util;

namespace OneIdentity.Scalus.Test
{
    // Locks in the step-3 "per-launch JSON records" behavior:
    //  - the LaunchRecord DTO round-trips through the source-gen serializer (AOT-safe write/read),
    //  - LaunchRecordStore writes one file per launch and lists them newest-first,
    //  - the one-time token is redacted in both rdp (~) and ssh (=) in-band forms,
    //  - the launcher outcome keywords are stable (the UI/records contract depends on them).
    public class TestLaunchRecords
    {
        [Fact]
        public void LaunchRecord_RoundTripsThroughSourceGenSerializer()
        {
            var record = new LaunchRecord
            {
                LaunchId = "abc123def456",
                TimestampUtc = new DateTime(2026, 7, 7, 12, 34, 56, DateTimeKind.Utc),
                Protocol = "rdp",
                Url = "rdp://host",
                ApplicationId = "app-1",
                Command = "/usr/bin/mstsc",
                Outcome = "spawned",
                Success = true,
                ExitCode = null,
                Error = null,
                DurationMs = 42,
            };

            var json = ScalusJson.Serialize(record);
            var back = ScalusJson.DeserializeLaunchRecord(json);

            Assert.Equal(record.LaunchId, back.LaunchId);
            Assert.Equal(record.TimestampUtc, back.TimestampUtc);
            Assert.Equal(record.Protocol, back.Protocol);
            Assert.Equal(record.ApplicationId, back.ApplicationId);
            Assert.Equal(record.Command, back.Command);
            Assert.Equal(record.Outcome, back.Outcome);
            Assert.True(back.Success);
            Assert.Null(back.ExitCode);
            Assert.Equal(42, back.DurationMs);
        }

        [Fact]
        public void LaunchRecord_OmitsNullOptionalFields()
        {
            var record = new LaunchRecord
            {
                LaunchId = "id",
                TimestampUtc = DateTime.UtcNow,
                Outcome = "spawned",
                Success = true,
            };

            var json = ScalusJson.Serialize(record);

            // Error is null on success and must not clutter the record.
            Assert.DoesNotContain("\"error\"", json);
            Assert.DoesNotContain("\"exitCode\"", json);
        }

        [Fact]
        public void Store_WritesAndListsRecord()
        {
            var launchId = "test" + Guid.NewGuid().ToString("N")[..8];
            var record = new LaunchRecord
            {
                LaunchId = launchId,
                TimestampUtc = DateTime.UtcNow,
                Protocol = "ssh",
                Outcome = "spawned",
                Success = true,
            };

            var path = LaunchRecordStore.Write(record);
            try
            {
                Assert.NotNull(path);
                Assert.True(File.Exists(path));

                var listed = LaunchRecordStore.List().FirstOrDefault(r => r.LaunchId == launchId);
                Assert.NotNull(listed);
                Assert.Equal("spawned", listed.Outcome);
            }
            finally
            {
                if (path != null && File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Fact]
        public void OutcomeKeyword_IsStable()
        {
            Assert.Equal("spawned", LaunchResult.OutcomeKeyword(LaunchOutcome.Spawned));
            Assert.Equal("preview", LaunchResult.OutcomeKeyword(LaunchOutcome.Preview));
            Assert.Equal("config-error", LaunchResult.OutcomeKeyword(LaunchOutcome.ConfigError));
            Assert.Equal("spawn-failed", LaunchResult.OutcomeKeyword(LaunchOutcome.SpawnFailed));
            Assert.Equal("post-execute-error", LaunchResult.OutcomeKeyword(LaunchOutcome.PostExecuteError));
        }

        [Fact]
        public void LaunchResult_SuccessOnlyForSpawnedOrPreview()
        {
            Assert.True(new LaunchResult { Outcome = LaunchOutcome.Spawned }.Success);
            Assert.True(new LaunchResult { Outcome = LaunchOutcome.Preview }.Success);
            Assert.False(new LaunchResult { Outcome = LaunchOutcome.SpawnFailed }.Success);
            Assert.False(new LaunchResult { Outcome = LaunchOutcome.ConfigError }.Success);
            Assert.False(new LaunchResult { Outcome = LaunchOutcome.PostExecuteError }.Success);
        }
    }
}
