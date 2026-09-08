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
            var created = await RequireSuccessAsync(() => admin.CreateKey(new CreateKeyParams
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

            var lookup = await RequireSuccessAsync(() => readOnly.GetKey(new GetKeyParams
            {
                ProductId = productId,
                LicenseKey = nodeKey
            }));
            Assert.Equal(productId, lookup.Data.License.ProductId);

            var activation = await RequireSuccessAsync(() => client.ActivateKey(new ActivateKeyParams
            {
                ProductId = productId,
                LicenseKey = nodeKey,
                HostId = nodeHostId,
                DeviceTag = "C# SDK CI"
            }));
            Assert.Equal(0, activation.Code);
            Assert.Equal(nodeHostId, activation.Metadata?["hostId"]?.ToString());

            var deactivated = await RequireSuccessAsync(() => client.DeactivateKey(new DeactivateKeyParams
            {
                ProductId = productId,
                LicenseKey = nodeKey,
                HostId = nodeHostId
            }));
            Assert.Equal(1, deactivated.DevicesRemoved);

            await RequireSuccessAsync(() => admin.UpdateKey(new UpdateKeyParams
            {
                ProductId = productId,
                LicenseKey = nodeKey,
                MaxActivations = 3
            }));
            await RequireSuccessAsync(() => admin.BlockKey(new BlockKeyParams
            {
                ProductId = productId,
                LicenseKey = nodeKey
            }));
            await RequireSuccessAsync(() => admin.UnblockKey(new UnblockKeyParams
            {
                ProductId = productId,
                LicenseKey = nodeKey
            }));

            var floatingCreated = await RequireSuccessAsync(() => admin.CreateKey(new CreateKeyParams
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

            var checkout = await RequireSuccessAsync(() => client.FloatingCheckout(new FloatingCheckoutParams
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
            var heartbeat = await RequireSuccessAsync(() => client.FloatingHeartbeat(new FloatingHeartbeatParams
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
            await RequireSuccessAsync(() => client.FloatingCheckin(new FloatingCheckinParams
            {
                ProductId = productId,
                LicenseKey = floatingKey,
                SessionId = checkout.SessionId,
                Timestamp = heartbeat.NextNonce,
                Signature = checkinSignature
            }));

            var signed = await RequireSuccessAsync(() => admin.SignKey(new SignKeyParams
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
                try
                {
                    await admin.BlockKey(new BlockKeyParams
                    {
                        ProductId = productId,
                        LicenseKey = nodeKey
                    });
                }
                catch
                {
                    // Best-effort cleanup: ignore rate-limit / network failures.
                }
            }

            if (floatingKey != null)
            {
                try
                {
                    await admin.BlockKey(new BlockKeyParams
                    {
                        ProductId = productId,
                        LicenseKey = floatingKey
                    });
                }
                catch
                {
                    // Best-effort cleanup: ignore rate-limit / network failures.
                }
            }
        }
    }

    private static bool IsRateLimited<T>(KeyMintResult<T> result)
    {
        var message = result.Error?.Message ?? string.Empty;
        if (message.Contains("Too many requests", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return result.Error?.Status == 429;
    }

    private static async Task<T> RequireSuccessAsync<T>(Func<Task<KeyMintResult<T>>> action, int maxAttempts = 5)
    {
        KeyMintResult<T>? lastResult = null;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            lastResult = await action().ConfigureAwait(false);
            if (lastResult.IsSuccess)
            {
                // Pace requests for free-plan workspaces (10 req/min org-global bucket).
                await Task.Delay(TimeSpan.FromSeconds(7)).ConfigureAwait(false);
                return Assert.IsType<T>(lastResult.Data);
            }

            if (!IsRateLimited(lastResult) || attempt == maxAttempts)
            {
                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(15)).ConfigureAwait(false);
        }

        Assert.True(lastResult!.IsSuccess, lastResult!.Error?.Message ?? "Keymint API request failed");
        return Assert.IsType<T>(lastResult.Data);
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
