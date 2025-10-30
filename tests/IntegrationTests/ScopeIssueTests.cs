using Azure.Core;
using Azure.Identity;

namespace IntegrationTests
{
    [TestClass]
    public class ScopeIssueTests
    {
        private const string Scope = "api://apim-managedidentity-nwe-i2jdr";
        //private const string Scope = "https://kvaisquicksdctaqld.vault.azure.net";

        [TestMethod]
        public async Task AzdWithoutDefaultSuffix()
        {
            var tokenCredential = new AzureDeveloperCliCredential();
            var tokenResult = await tokenCredential.GetTokenAsync(new TokenRequestContext([Scope]));

            Assert.IsNotNull(tokenResult);
            Assert.IsNotNull(tokenResult.Token);
        }

        [TestMethod]
        public async Task AzWithoutDefaultSuffix()
        {
            var tokenCredential = new AzureCliCredential();
            var tokenResult = await tokenCredential.GetTokenAsync(new TokenRequestContext([Scope]));

            Assert.IsNotNull(tokenResult);
            Assert.IsNotNull(tokenResult.Token);
        }

        [TestMethod]
        public async Task AzdWithDefaultSuffix()
        {
            var tokenCredential = new AzureDeveloperCliCredential();
            var tokenResult = await tokenCredential.GetTokenAsync(new TokenRequestContext([$"{Scope}/.default"]));

            Assert.IsNotNull(tokenResult);
            Assert.IsNotNull(tokenResult.Token);
        }

        [TestMethod]
        public async Task AzWithDefaultSuffix()
        {
            var tokenCredential = new AzureCliCredential();
            var tokenResult = await tokenCredential.GetTokenAsync(new TokenRequestContext([$"{Scope}/.default"]));

            Assert.IsNotNull(tokenResult);
            Assert.IsNotNull(tokenResult.Token);
        }
    }
}
