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
    public async Task DeleteKey_sends_DELETE_with_license_in_json_body_instead_of_url()
    {
        HttpRequestMessage? capturedRequest = null;
        string? requestBody = null;
        var handler = new StubHandler(request =>
        {
            capturedRequest = request;
            requestBody = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return JsonResponse(new { message = "Key deleted", code = 0 });
        });
        var sdk = CreateSdk(handler);

        var result = await sdk.DeleteKey(new DeleteKeyParams
        {
            ProductId = "product_123",
            LicenseKey = "secret/license+key"
        });

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal("Key deleted", result.Data!.Message);
        Assert.Equal(0, result.Data.Code);

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Delete, capturedRequest!.Method);
        Assert.Equal("/key", capturedRequest.RequestUri!.AbsolutePath);
        Assert.Equal(string.Empty, capturedRequest.RequestUri.Query);
        Assert.DoesNotContain("secret", capturedRequest.RequestUri.ToString());
        Assert.False(capturedRequest.Headers.Contains("x-license-key"));
        Assert.Equal("Bearer", capturedRequest.Headers.Authorization?.Scheme);
        Assert.Equal("admin_test", capturedRequest.Headers.Authorization?.Parameter);
        Assert.Equal("application/json", capturedRequest.Content!.Headers.ContentType?.MediaType);

        using var body = JsonDocument.Parse(requestBody!);
        Assert.Equal("product_123", body.RootElement.GetProperty("productId").GetString());
        Assert.Equal("secret/license+key", body.RootElement.GetProperty("licenseKey").GetString());
    }

    [Fact]
    public async Task DeleteKey_returns_api_error_for_unknown_key()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(
                "{\"message\":\"Invalid license key\",\"code\":1}",
                Encoding.UTF8,
                "application/json")
        });
        var sdk = CreateSdk(handler);

        var result = await sdk.DeleteKey(new DeleteKeyParams
        {
            ProductId = "product_123",
            LicenseKey = "missing_key"
        });

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Equal("Invalid license key", result.Error!.Message);
        Assert.Equal(1, result.Error.Code);
        Assert.Equal(404, result.Error.Status);
    }

    [Fact]
    public async Task DeleteKey_rejects_missing_parameters_without_a_request()
    {
        var called = false;
        var sdk = CreateSdk(new StubHandler(_ =>
        {
            called = true;
            return JsonResponse(new { message = "Key deleted", code = 0 });
        }));

        var result = await sdk.DeleteKey(new DeleteKeyParams { ProductId = "product_123", LicenseKey = " " });

        Assert.False(result.IsSuccess);
        Assert.False(called);
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
    public async Task ActivateKey_deserializes_expiration_date()
    {
        var handler = new StubHandler(_ => RawJsonResponse(
            "{\"code\":0,\"message\":\"License valid\",\"expirationDate\":\"2027-03-01T00:00:00.000Z\"}"));
        var sdk = CreateSdk(handler);

        var result = await sdk.ActivateKey(new ActivateKeyParams
        {
            ProductId = "product_123",
            LicenseKey = "license_123"
        });

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(new DateTimeOffset(2027, 3, 1, 0, 0, 0, TimeSpan.Zero), result.Data!.ExpirationDate);
        Assert.Equal(TimeSpan.Zero, result.Data.ExpirationDate!.Value.Offset);
    }

    [Theory]
    [InlineData("{\"code\":0,\"message\":\"License valid\",\"expirationDate\":null}")]
    [InlineData("{\"code\":0,\"message\":\"License valid\"}")]
    public async Task ActivateKey_treats_null_or_missing_expiration_date_as_never_expires(string json)
    {
        var sdk = CreateSdk(new StubHandler(_ => RawJsonResponse(json)));

        var result = await sdk.ActivateKey(new ActivateKeyParams
        {
            ProductId = "product_123",
            LicenseKey = "license_123"
        });

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Null(result.Data!.ExpirationDate);
    }

    [Theory]
    [InlineData("\"2027-03-01T00:00:00.000Z\"", true)]
    [InlineData("null", false)]
    public async Task FloatingCheckout_deserializes_license_expiration_separately_from_seat_lease(string expirationJson, bool hasExpiry)
    {
        var json = "{\"code\":0,\"message\":\"Seat checked out\",\"sessionId\":\"session_123\"," +
                   "\"sessionSecret\":\"secret_123\",\"nextNonce\":\"nonce_1\",\"expiresAt\":\"2026-09-08T01:00:00Z\"," +
                   "\"heartbeatInterval\":60,\"expirationDate\":" + expirationJson + "}";
        var sdk = CreateSdk(new StubHandler(_ => RawJsonResponse(json)));

        var result = await sdk.FloatingCheckout(new FloatingCheckoutParams
        {
            ProductId = "product_123",
            LicenseKey = "license_123",
            HostId = "host_123"
        });

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal("2026-09-08T01:00:00Z", result.Data!.ExpiresAt);
        if (hasExpiry)
        {
            Assert.Equal(new DateTimeOffset(2027, 3, 1, 0, 0, 0, TimeSpan.Zero), result.Data.ExpirationDate);
        }
        else
        {
            Assert.Null(result.Data.ExpirationDate);
        }
    }

    [Fact]
    public void ExpirationDate_round_trips_through_System_Text_Json()
    {
        var original = new ActivateKeyResponse
        {
            Code = 0,
            Message = "License valid",
            ExpirationDate = new DateTimeOffset(2027, 3, 1, 0, 0, 0, TimeSpan.Zero)
        };

        var roundTripped = JsonSerializer.Deserialize<ActivateKeyResponse>(JsonSerializer.Serialize(original));

        Assert.Equal(original.ExpirationDate, roundTripped!.ExpirationDate);
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

    private static HttpResponseMessage RawJsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
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
