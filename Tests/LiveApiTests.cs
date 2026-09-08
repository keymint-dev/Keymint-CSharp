using KeyMint.Services;
using Xunit;

namespace KeyMint.Tests;

public class LiveApiTests
{
    [Fact]
    [Trait("Category", "Live")]
    public async Task Current_license_workflows_work_end_to_end()
    {
        var adminKey = RequiredEnvironmentVariable("KEYMINT_TEST_ADMIN_API_KEY");
        var readOnlyKey = RequiredEnvironmentVariable("KEYMINT_TEST_READONLY_API_KEY");
        var clientKey = RequiredEnvironmentVariable("KEYMINT_TEST_CLIENT_API_KEY");
        var productId = RequiredEnvironmentVariable("KEYMINT_TEST_PRODUCT_ID");
        var baseUrl = Environment.GetEnvironmentVariable("KEYMINT_TEST_BASE_URL")
            ?? "https://api.keymint.dev";

        var admin = new KeyMintSDK(adminKey, baseUrl);
        var readOnly = new KeyMintSDK(readOnlyKey, baseUrl);
        var client = new KeyMintSDK(clientKey, baseUrl);
        var runId = Guid.NewGuid().ToString("N");
        var nodeHostId = $"ci-node-{runId}";
        string? nodeKey = null;
        string? floatingKey = null;

        try
        {
            var created = RequireSuccess(await admin.CreateKey(new CreateKeyParams
            {
                ProductId = productId,
                MaxActivations = "2",
                Metadata = new Dictionary<string, object>
                {
                    ["purpose"] = "csharp-sdk-live-test",
                    ["runId"] = runId
                }
            }));
            nodeKey = Assert.IsType<string>(created.Key);

            var lookup = RequireSuccess(await readOnly.GetKey(new GetKeyParams
            {
                ProductId = productId,
                LicenseKey = nodeKey
            }));
            Assert.Equal(productId, lookup.Data.License.ProductId);

            var activation = RequireSuccess(await client.ActivateKey(new ActivateKeyParams
            {
                ProductId = productId,
                LicenseKey = nodeKey,
                HostId = nodeHostId,
                DeviceTag = "C# SDK CI"
            }));
            Assert.Equal(0, activation.Code);
            Assert.Equal(nodeHostId, activation.Metadata?["hostId"]?.ToString());

            var deactivated = RequireSuccess(await client.DeactivateKey(new DeactivateKeyParams
            {
                ProductId = productId,
                LicenseKey = nodeKey,
                HostId = nodeHostId
            }));
            Assert.Equal(1, deactivated.DevicesRemoved);

            RequireSuccess(await admin.UpdateKey(new UpdateKeyParams
            {
                ProductId = productId,
                LicenseKey = nodeKey,
                MaxActivations = 3
            }));
            RequireSuccess(await admin.BlockKey(new BlockKeyParams
            {
                ProductId = productId,
                LicenseKey = nodeKey
            }));
            RequireSuccess(await admin.UnblockKey(new UnblockKeyParams
            {
                ProductId = productId,
                LicenseKey = nodeKey
            }));

            var floatingCreated = RequireSuccess(await admin.CreateKey(new CreateKeyParams
            {
                ProductId = productId,
                LicenseType = "floating",
                MaxConcurrentSessions = 1,
                HeartbeatInterval = 60,
                SessionLeaseDuration = 300,
                Metadata = new Dictionary<string, object>
                {
                    ["purpose"] = "csharp-sdk-floating-live-test",
                    ["runId"] = runId
                }
            }));
            floatingKey = Assert.IsType<string>(floatingCreated.Key);

            var checkout = RequireSuccess(await client.FloatingCheckout(new FloatingCheckoutParams
            {
                ProductId = productId,
                LicenseKey = floatingKey,
                HostId = $"ci-floating-{runId}",
                DeviceTag = "C# SDK CI floating"
            }));
            Assert.False(string.IsNullOrWhiteSpace(checkout.SessionSecret));

            var heartbeatSignature = KeyMintIdentity.GenerateSessionSignature(
                checkout.SessionId,
                checkout.NextNonce,
                checkout.SessionSecret);
            var heartbeat = RequireSuccess(await client.FloatingHeartbeat(new FloatingHeartbeatParams
            {
                ProductId = productId,
                LicenseKey = floatingKey,
                SessionId = checkout.SessionId,
                Timestamp = checkout.NextNonce,
                Signature = heartbeatSignature
            }));

            var checkinSignature = KeyMintIdentity.GenerateSessionSignature(
                checkout.SessionId,
                heartbeat.NextNonce,
                checkout.SessionSecret);
            RequireSuccess(await client.FloatingCheckin(new FloatingCheckinParams
            {
                ProductId = productId,
                LicenseKey = floatingKey,
                SessionId = checkout.SessionId,
                Timestamp = heartbeat.NextNonce,
                Signature = checkinSignature
            }));

            var signed = RequireSuccess(await admin.SignKey(new SignKeyParams
            {
                ProductId = productId,
                LicenseKey = nodeKey,
                HostId = nodeHostId,
                Ttl = 300
            }));
            Assert.Contains("signedKey", signed.File, StringComparison.Ordinal);
            Assert.Contains("keyId", signed.File, StringComparison.Ordinal);
        }
        finally
        {
            if (nodeKey != null)
            {
                await admin.BlockKey(new BlockKeyParams
                {
                    ProductId = productId,
                    LicenseKey = nodeKey
                });
            }

            if (floatingKey != null)
            {
                await admin.BlockKey(new BlockKeyParams
                {
                    ProductId = productId,
                    LicenseKey = floatingKey
                });
            }
        }
    }

    private static T RequireSuccess<T>(KeyMintResult<T> result)
    {
        Assert.True(result.IsSuccess, result.Error?.Message ?? "Keymint API request failed");
        return Assert.IsType<T>(result.Data);
    }

    private static string RequiredEnvironmentVariable(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        Assert.False(string.IsNullOrWhiteSpace(value), $"Missing required environment variable: {name}");
        return value!;
    }
}
