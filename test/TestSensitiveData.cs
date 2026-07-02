using Xunit;

namespace OneIdentity.Scalus.Test
{
    // Guards the log-redaction of the Safeguard/SPS one-time token so it never
    // reaches the log in clear text regardless of the in-band separator style.
    public class TestSensitiveData
    {
        [Fact]
        public void RedactsSshStyleToken()
        {
            var input = "vaultaddress=10.5.32.162@token=ONETIMETOKEN@svc-admin@app01.example.com:3389";
            var result = SensitiveData.Redact(input);

            Assert.DoesNotContain("ONETIMETOKEN", result);
            Assert.Contains("token=***", result);
            Assert.Contains("svc-admin", result);
        }

        [Fact]
        public void RedactsRdpStyleToken()
        {
            var input = "gw\\account~acct%token~ONETIMETOKEN%svc-admin%app01.example.com";
            var result = SensitiveData.Redact(input);

            Assert.DoesNotContain("ONETIMETOKEN", result);
            Assert.Contains("token~***", result);
        }

        [Fact]
        public void LeavesUnrelatedTextUntouched()
        {
            Assert.Equal("ssh://user@host:22", SensitiveData.Redact("ssh://user@host:22"));
        }

        [Fact]
        public void HandlesNullAndEmpty()
        {
            Assert.Null(SensitiveData.Redact(null));
            Assert.Equal(string.Empty, SensitiveData.Redact(string.Empty));
        }
    }
}
