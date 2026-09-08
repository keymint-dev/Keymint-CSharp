using System.Net;
using System.Text;
using System.Text.Json;
using KeyMint.Services;
using Xunit;

namespace KeyMint.Tests;

public class ApiContractTests
{
    [Fact]
    public async Task SignKey_deserializes_the_current_file_string_response()
    {
        const string offlineFile = "{\"signedDate\":\"2026-09-08T00:00:00Z\",\"signedKey\":\"signature\",\"keyId\":\"key_123\"}";
        var handler = new StubHandler(_ => JsonResponse(new { file = offlineFile }));
        var sdk = CreateSdk(handler);

        var result = await sdk.SignKey(new SignKeyParams
        {
            ProductId = "product_123",
            LicenseKey = "license_123",
            HostId = "host_123"
        });

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(offlineFile, result.Data!.File);
    }

    [Fact]
    public async Task GetKey_sends_license_in_header_instead_of_url()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new StubHandler(request =>
        {
            capturedRequest = request;
            return JsonResponse(new
            {
                code = 0,
                data = new
                {
                    license = new
                    {
                        id = "id_123",
                        key = "masked",
                        productId = "product 123",
                        maxActivations = 1,
                        activations = 0,
                        devices = Array.Empty<object>(),
                        activated = false
                    },
                    customer = (object?)null
                }
            });
        });
        var sdk = CreateSdk(handler);

        var result = await sdk.GetKey(new GetKeyParams
        {
            ProductId = "product 123",
            LicenseKey = "secret/license+key"
        });

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.NotNull(capturedRequest);
        Assert.DoesNotContain("secret", capturedRequest!.RequestUri!.Query);
        Assert.Contains("productId=product+123", capturedRequest.RequestUri.Query);
        Assert.Equal("secret/license+key", capturedRequest.Headers.GetValues("x-license-key").Single());
    }

    [Fact]
    public async Task ActivateKey_deserializes_metadata_and_version_fields()
    {
        var handler = new StubHandler(_ => JsonResponse(new
        {
            code = 0,
            message = "License valid",
            metadata = new { tier = "pro" },
            versionId = "version_123",
            version = new { version = "2.0.0" }
        }));
        var sdk = CreateSdk(handler);

        var result = await sdk.ActivateKey(new ActivateKeyParams
        {
            ProductId = "product_123",
            LicenseKey = "license_123"
        });

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal("version_123", result.Data!.VersionId);
        Assert.Equal("2.0.0", result.Data.Version!.Version);
        Assert.Equal("pro", ((JsonElement)result.Data.Metadata!["tier"]).GetString());
    }

    [Fact]
    public async Task CreateKey_deserializes_bulk_keys()
    {
        var handler = new StubHandler(_ => JsonResponse(new
        {
            code = 0,
            keys = new[] { "license_1", "license_2" }
        }));
        var sdk = CreateSdk(handler);

        var result = await sdk.CreateKey(new CreateKeyParams
        {
            ProductId = "product_123",
            AmountKeys = "2"
        });

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(new[] { "license_1", "license_2" }, result.Data!.Keys);
        Assert.Null(result.Data.Key);
    }

    [Fact]
    public async Task FloatingCheckout_serializes_session_renewal_credentials()
    {
        string? requestBody = null;
        var handler = new StubHandler(request =>
        {
            requestBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return JsonResponse(new
            {
                code = 0,
                message = "Session renewed",
                sessionId = "session_123",
                sessionSecret = "secret_123",
                nextNonce = "nonce_2",
                expiresAt = "2026-09-08T01:00:00Z",
                heartbeatInterval = 60
            });
        });
        var sdk = CreateSdk(handler);

        var result = await sdk.FloatingCheckout(new FloatingCheckoutParams
        {
            ProductId = "product_123",
            LicenseKey = "license_123",
            HostId = "host_123",
            Timestamp = "nonce_1",
            Signature = "signature_1"
        });

        Assert.True(result.IsSuccess, result.Error?.Message);
        using var body = JsonDocument.Parse(requestBody!);
        Assert.Equal("nonce_1", body.RootElement.GetProperty("timestamp").GetString());
        Assert.Equal("signature_1", body.RootElement.GetProperty("signature").GetString());
    }

    [Fact]
    public async Task Api_errors_expose_the_current_nested_error_message()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent(
                "{\"success\":false,\"error\":{\"code\":\"CUSTOMER_EMAIL_EXISTS\",\"message\":\"Customer email already exists in team\"},\"code\":1}",
                Encoding.UTF8,
                "application/json")
        });
        var sdk = CreateSdk(handler);

        var result = await sdk.CreateKey(new CreateKeyParams { ProductId = "product_123" });

        Assert.False(result.IsSuccess);
        Assert.Equal("Customer email already exists in team", result.Error!.Message);
        Assert.Equal("CUSTOMER_EMAIL_EXISTS", result.Error.Error!.Code);
        Assert.Equal(409, result.Error.Status);
    }

    private static KeyMintSDK CreateSdk(HttpMessageHandler handler)
    {
        return new KeyMintSDK("admin_test", new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.keymint.dev")
        });
    }

    private static HttpResponseMessage JsonResponse(object value)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json")
        };
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(responder(request));
        }
    }
}
