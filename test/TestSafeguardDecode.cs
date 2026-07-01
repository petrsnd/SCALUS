using System.Collections.Generic;
using Xunit;
using OneIdentity.Scalus.UrlParser;
using static OneIdentity.Scalus.Dto.ParserConfigDefinitions;

namespace OneIdentity.Scalus.Test
{
    // Characterization tests: pin the current behavior of the Safeguard in-band
    // username decode so the Core extraction/refactor is provably behavior-preserving.
    public class TestSafeguardDecode
    {
        private static IDictionary<Token, string> Decode(string user)
        {
            var dict = new Dictionary<Token, string> { { Token.User, user } };
            foreach (Token t in System.Enum.GetValues(typeof(Token)))
            {
                if (!dict.ContainsKey(t))
                {
                    dict[t] = string.Empty;
                }
            }

            BaseParser.GetSafeguardUserValue(dict);
            return dict;
        }

        [Fact]
        public void DecodesVaultTokenTargetUserAndHostWithPort()
        {
            var dict = Decode("vaultaddress=10.5.32.162@token=ONETIMETOKEN@svc-admin@app01.example.com:3389");

            Assert.Equal("10.5.32.162", dict[Token.Vault]);
            Assert.Equal("ONETIMETOKEN", dict[Token.Token]);
            Assert.Equal("svc-admin", dict[Token.TargetUser]);
            Assert.Equal("app01.example.com", dict[Token.TargetHost]);
            Assert.Equal("3389", dict[Token.TargetPort]);
        }

        [Fact]
        public void DecodesAccountAndAssetWhenPresent()
        {
            var dict = Decode("vaultaddress=vault.local@account=dbadmin@asset=oracle01@token=TOK@dbadmin@oracle01.example.com");

            Assert.Equal("vault.local", dict[Token.Vault]);
            Assert.Equal("dbadmin", dict[Token.Account]);
            Assert.Equal("oracle01", dict[Token.Asset]);
            Assert.Equal("TOK", dict[Token.Token]);
        }

        [Fact]
        public void HostWithoutPortLeavesTargetPortEmpty()
        {
            var dict = Decode("vaultaddress=v@token=TOK@user1@plainhost");

            Assert.Equal("plainhost", dict[Token.TargetHost]);
            Assert.Null(dict[Token.TargetPort]);
        }
    }
}
