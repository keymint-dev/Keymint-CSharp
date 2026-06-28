using System;
using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Web;
using Microsoft.Extensions.Logging;


namespace KeyMint.Services;

/// <summary>
/// Exception thrown when the KeyMint API returns an error response
/// </summary>
public class KeyMintApiException : Exception
{
    public int ErrorCode { get; }
    public HttpStatusCode? StatusCode { get; }
    public KeyMintApiError? ApiError { get; set; }
    
    public KeyMintApiException(string message, int errorCode, HttpStatusCode? statusCode = null)
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }
    
    public KeyMintApiException(string message, int errorCode, HttpStatusCode? statusCode, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }
}

public class KeyMintSDK
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<KeyMintSDK>? _logger;

    public KeyMintSDK(string apiKey, string baseUrl = "https://api.keymint.dev", ILogger<KeyMintSDK>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(apiKey, nameof(apiKey));

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl)
        };

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        _logger = logger;
    }

    /// <summary>
    /// Generic method to handle POST requests
    /// </summary>
    private async Task<KeyMintResult<T>> HandleRequest<T>(string endpoint, object parameters, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(parameters)
            };
            if (options?.IdempotencyKey != null)
            {
                request.Headers.Add("Idempotency-Key", options.IdempotencyKey);
            }
            var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            return await HandleResponse<T>(response).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in POST {Endpoint}", endpoint);
            return KeyMintResult<T>.Failure(new KeyMintApiError { Message = ex.Message, Code = -1 });
        }
    }

    /// <summary>
    /// Generic method to handle GET requests
    /// </summary>
    private async Task<KeyMintResult<T>> HandleGetRequest<T>(string endpoint, Dictionary<string, string>? queryParams = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var queryString = queryParams != null ? ToQueryString(queryParams) : string.Empty;
            var fullEndpoint = string.IsNullOrEmpty(queryString) ? endpoint : $"{endpoint}?{queryString}";
            var response = await _httpClient.GetAsync(fullEndpoint, cancellationToken).ConfigureAwait(false);
            return await HandleResponse<T>(response).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GET {Endpoint}", endpoint);
            return KeyMintResult<T>.Failure(new KeyMintApiError { Message = ex.Message, Code = -1 });
        }
    }

    /// <summary>
    /// Generic method to handle DELETE requests
    /// </summary>
    private async Task<KeyMintResult<T>> HandleDeleteRequest<T>(string endpoint, Dictionary<string, string>? queryParams = null, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var queryString = queryParams != null ? ToQueryString(queryParams) : string.Empty;
            var fullEndpoint = string.IsNullOrEmpty(queryString) ? endpoint : $"{endpoint}?{queryString}";
            var request = new HttpRequestMessage(HttpMethod.Delete, fullEndpoint);
            if (options?.IdempotencyKey != null)
            {
                request.Headers.Add("Idempotency-Key", options.IdempotencyKey);
            }
            var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            return await HandleResponse<T>(response).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in DELETE {Endpoint}", endpoint);
            return KeyMintResult<T>.Failure(new KeyMintApiError { Message = ex.Message, Code = -1 });
        }
    }

    /// <summary>
    /// Generic method to handle PUT requests
    /// </summary>
    private async Task<KeyMintResult<T>> HandlePutRequest<T>(string endpoint, object parameters, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Put, endpoint)
            {
                Content = JsonContent.Create(parameters)
            };
            if (options?.IdempotencyKey != null)
            {
                request.Headers.Add("Idempotency-Key", options.IdempotencyKey);
            }
            var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            return await HandleResponse<T>(response).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in PUT {Endpoint}", endpoint);
            return KeyMintResult<T>.Failure(new KeyMintApiError { Message = ex.Message, Code = -1 });
        }
    }

    /// <summary>
    /// Handles HTTP response and deserializes the content, returning a result object (never throws for API errors)
    /// </summary>
    private async Task<KeyMintResult<T>> HandleResponse<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            string rawError = await response.Content.ReadAsStringAsync();
            try
            {
                var errorContent = System.Text.Json.JsonSerializer.Deserialize<KeyMintApiError>(rawError);
                if (errorContent != null)
                {
                    _logger?.LogError("API Error: {StatusCode} {ReasonPhrase} | {Error}", (int)response.StatusCode, response.ReasonPhrase, rawError);
                    return KeyMintResult<T>.Failure(errorContent);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to parse error response. Status: {StatusCode} {ReasonPhrase}. Raw: {RawError}", (int)response.StatusCode, response.ReasonPhrase, rawError);
                return KeyMintResult<T>.Failure(new KeyMintApiError {
                    Message = $"HTTP error: {response.StatusCode} - {response.ReasonPhrase}. Response: {rawError}",
                    Code = -1,
                    Status = (int?)response.StatusCode
                });
            }
            _logger?.LogError("HTTP error: {StatusCode} - {ReasonPhrase}. Response: {RawError}", (int)response.StatusCode, response.ReasonPhrase, rawError);
            return KeyMintResult<T>.Failure(new KeyMintApiError {
                Message = $"HTTP error: {response.StatusCode} - {response.ReasonPhrase}. Response: {rawError}",
                Code = -1,
                Status = (int?)response.StatusCode
            });
        }

        var rawJson = await response.Content.ReadAsStringAsync();
        try
        {
            var content = System.Text.Json.JsonSerializer.Deserialize<T>(rawJson);
            if (content == null)
            {
                return KeyMintResult<T>.Failure(new KeyMintApiError {
                    Message = "Failed to deserialize API response",
                    Code = -1,
                    Status = (int?)response.StatusCode
                });
            }
            return KeyMintResult<T>.Success(content);
        }
        catch (System.Text.Json.JsonException jsonEx)
        {
            _logger?.LogError(jsonEx, "Failed to deserialize response to {Type}. Raw JSON: {RawJson}", typeof(T), rawJson);
            return KeyMintResult<T>.Failure(new KeyMintApiError {
                Message = $"Deserialization error: {jsonEx.Message}",
                Code = -1,
                Status = (int?)response.StatusCode
            });
        }
    }

    /// <summary>
    /// Handles exceptions and converts them to appropriate error types
    /// </summary>
    private static KeyMintApiException HandleError(Exception ex)
    {
        if (ex is KeyMintApiException apiEx)
        {
            return apiEx;
        }
        else if (ex is HttpRequestException httpEx)
        {
            
            return new KeyMintApiException(httpEx.Message, -1, null, httpEx);
        }
        else
        {
            return new KeyMintApiException("An unexpected error occurred", -1, null, ex);
        }
    }

    /// <summary>
    /// Converts dictionary to query string
    /// </summary>
    private static string ToQueryString(Dictionary<string, string> parameters)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        foreach (var param in parameters)
        {
            query[param.Key] = param.Value;
        }
        return query.ToString() ?? string.Empty;
    }

    // API Methods

    /// <summary>
    /// Creates a new license key.
    /// </summary>
    /// <param name="parameters">The parameters for key creation.</param>
    /// <param name="options">Optional request configurations (e.g. idempotency keys).</param>
    /// <returns>The created key response or error.</returns>
    public async Task<KeyMintResult<CreateKeyResponse>> CreateKey(CreateKeyParams parameters, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (parameters == null || !parameters.IsValid())
            return KeyMintResult<CreateKeyResponse>.Failure(new KeyMintApiError { Message = "Invalid CreateKeyParams", Code = -1 });
        return await HandleRequest<CreateKeyResponse>("/key", parameters, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Activates a license key.
    /// </summary>
    /// <param name="parameters">The parameters for key activation.</param>
    /// <param name="options">Optional request configurations (e.g. idempotency keys).</param>
    /// <returns>The activation response or error.</returns>
    public async Task<KeyMintResult<ActivateKeyResponse>> ActivateKey(ActivateKeyParams parameters, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (parameters == null || !parameters.IsValid())
            return KeyMintResult<ActivateKeyResponse>.Failure(new KeyMintApiError { Message = "Invalid ActivateKeyParams", Code = -1 });
        return await HandleRequest<ActivateKeyResponse>("/key/activate", parameters, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Deactivates a license key.
    /// </summary>
    /// <param name="parameters">The parameters for key deactivation.</param>
    /// <param name="options">Optional request configurations (e.g. idempotency keys).</param>
    /// <returns>The deactivation response.</returns>
    /// <exception cref="KeyMintApiException">Thrown if the API returns an error.</exception>
    public async Task<KeyMintResult<DeactivateKeyResponse>> DeactivateKey(DeactivateKeyParams parameters, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (parameters == null || !parameters.IsValid())
            return KeyMintResult<DeactivateKeyResponse>.Failure(new KeyMintApiError { Message = "Invalid DeactivateKeyParams", Code = -1 });
        return await HandleRequest<DeactivateKeyResponse>("/key/deactivate", parameters, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Checks out a floating license seat.
    /// </summary>
    /// <param name="parameters">The parameters for checking out the license.</param>
    /// <param name="options">Optional request configurations (e.g. idempotency keys).</param>
    /// <returns>The checkout response containing sessionId and sessionSecret.</returns>
    public async Task<KeyMintResult<FloatingCheckoutResponse>> FloatingCheckout(FloatingCheckoutParams parameters, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (parameters == null || !parameters.IsValid())
            return KeyMintResult<FloatingCheckoutResponse>.Failure(new KeyMintApiError { Message = "Invalid FloatingCheckoutParams", Code = -1 });
        return await HandleRequest<FloatingCheckoutResponse>("/key/checkout", parameters, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a heartbeat to keep a floating license session alive.
    /// </summary>
    /// <param name="parameters">The parameters for the heartbeat.</param>
    /// <param name="options">Optional request configurations (e.g. idempotency keys).</param>
    /// <returns>The heartbeat response with extended lease expiry.</returns>
    public async Task<KeyMintResult<FloatingHeartbeatResponse>> FloatingHeartbeat(FloatingHeartbeatParams parameters, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (parameters == null || !parameters.IsValid())
            return KeyMintResult<FloatingHeartbeatResponse>.Failure(new KeyMintApiError { Message = "Invalid FloatingHeartbeatParams", Code = -1 });
        return await HandleRequest<FloatingHeartbeatResponse>("/key/heartbeat", parameters, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Checks in a floating license session, releasing the seat.
    /// </summary>
    /// <param name="parameters">The parameters for checking in the license.</param>
    /// <param name="options">Optional request configurations (e.g. idempotency keys).</param>
    /// <returns>The checkin response confirmation.</returns>
    public async Task<KeyMintResult<FloatingCheckinResponse>> FloatingCheckin(FloatingCheckinParams parameters, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (parameters == null || !parameters.IsValid())
            return KeyMintResult<FloatingCheckinResponse>.Failure(new KeyMintApiError { Message = "Invalid FloatingCheckinParams", Code = -1 });
        return await HandleRequest<FloatingCheckinResponse>("/key/checkin", parameters, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves a license key by product and license key.
    /// </summary>
    /// <param name="parameters">The parameters for key retrieval.</param>
    /// <returns>The key response.</returns>
    /// <exception cref="KeyMintApiException">Thrown if the API returns an error.</exception>
    public async Task<KeyMintResult<GetKeyResponse>> GetKey(GetKeyParams parameters, CancellationToken cancellationToken = default)
    {
        if (parameters == null || !parameters.IsValid())
            return KeyMintResult<GetKeyResponse>.Failure(new KeyMintApiError { Message = "Invalid GetKeyParams", Code = -1 });
        var fullEndpoint = $"/key?productId={parameters.ProductId}&licenseKey={parameters.LicenseKey}";
        var response = await _httpClient.GetAsync(fullEndpoint, cancellationToken).ConfigureAwait(false);
        return await HandleResponse<GetKeyResponse>(response).ConfigureAwait(false);
    }

    /// <summary>
    /// Blocks a license key.
    /// </summary>
    /// <param name="parameters">The parameters for blocking the key.</param>
    /// <param name="options">Optional request configurations (e.g. idempotency keys).</param>
    /// <returns>The block response.</returns>
    /// <exception cref="KeyMintApiException">Thrown if the API returns an error.</exception>
    public async Task<KeyMintResult<BlockKeyResponse>> BlockKey(BlockKeyParams parameters, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (parameters == null || !parameters.IsValid())
            return KeyMintResult<BlockKeyResponse>.Failure(new KeyMintApiError { Message = "Invalid BlockKeyParams", Code = -1 });
        return await HandleRequest<BlockKeyResponse>("/key/block", parameters, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Unblocks a license key.
    /// </summary>
    /// <param name="parameters">The parameters for unblocking the key.</param>
    /// <param name="options">Optional request configurations (e.g. idempotency keys).</param>
    /// <returns>The unblock response.</returns>
    /// <exception cref="KeyMintApiException">Thrown if the API returns an error.</exception>
    public async Task<KeyMintResult<UnblockKeyResponse>> UnblockKey(UnblockKeyParams parameters, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (parameters == null || !parameters.IsValid())
            return KeyMintResult<UnblockKeyResponse>.Failure(new KeyMintApiError { Message = "Invalid UnblockKeyParams", Code = -1 });
        return await HandleRequest<UnblockKeyResponse>("/key/unblock", parameters, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a new customer.
    /// </summary>
    /// <param name="parameters">The parameters for customer creation.</param>
    /// <param name="options">Optional request configurations (e.g. idempotency keys).</param>
    /// <returns>The created customer response.</returns>
    /// <exception cref="KeyMintApiException">Thrown if the API returns an error.</exception>
    public async Task<KeyMintResult<CreateCustomerResponse>> CreateCustomer(CreateCustomerParams parameters, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (parameters == null || !parameters.IsValid())
            return KeyMintResult<CreateCustomerResponse>.Failure(new KeyMintApiError { Message = "Invalid CreateCustomerParams", Code = -1 });
        return await HandleRequest<CreateCustomerResponse>("/customer", parameters, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves all customers.
    /// </summary>
    /// <returns>The response containing all customers.</returns>
    /// <exception cref="KeyMintApiException">Thrown if the API returns an error.</exception>
    public async Task<KeyMintResult<GetAllCustomersResponse>> GetAllCustomers(CancellationToken cancellationToken = default)
    {
        return await HandleGetRequest<GetAllCustomersResponse>("/customer", null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves a customer and their keys by customer ID.
    /// </summary>
    /// <param name="parameters">The parameters for retrieval.</param>
    /// <returns>The customer with keys response.</returns>
    /// <exception cref="KeyMintApiException">Thrown if the API returns an error.</exception>
    public async Task<KeyMintResult<GetCustomerWithKeysResponse>> GetCustomerWithKeys(GetCustomerWithKeysParams parameters, CancellationToken cancellationToken = default)
    {
        if (parameters == null || !parameters.IsValid())
            return KeyMintResult<GetCustomerWithKeysResponse>.Failure(new KeyMintApiError { Message = "Invalid GetCustomerWithKeysParams", Code = -1 });
        var queryParams = new Dictionary<string, string>
        {
            { "customerId", parameters.CustomerId }
        };
        return await HandleGetRequest<GetCustomerWithKeysResponse>("/customer/keys", queryParams, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates a customer by ID.
    /// </summary>
    /// <param name="parameters">The parameters for updating the customer.</param>
    /// <param name="options">Optional request configurations (e.g. idempotency keys).</param>
    /// <returns>The update response.</returns>
    /// <exception cref="KeyMintApiException">Thrown if the API returns an error.</exception>
    public async Task<KeyMintResult<UpdateCustomerResponse>> UpdateCustomer(UpdateCustomerParams parameters, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (parameters == null || !parameters.IsValid())
            return KeyMintResult<UpdateCustomerResponse>.Failure(new KeyMintApiError { Message = "Invalid UpdateCustomerParams", Code = -1 });
        return await HandlePutRequest<UpdateCustomerResponse>("/customer/by-id", parameters, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes a customer by ID.
    /// </summary>
    /// <param name="parameters">The parameters for deletion.</param>
    /// <param name="options">Optional request configurations (e.g. idempotency keys).</param>
    /// <returns>The delete response.</returns>
    /// <exception cref="KeyMintApiException">Thrown if the API returns an error.</exception>
    public async Task<KeyMintResult<DeleteCustomerResponse>> DeleteCustomer(DeleteCustomerParams parameters, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (parameters == null || !parameters.IsValid())
            return KeyMintResult<DeleteCustomerResponse>.Failure(new KeyMintApiError { Message = "Invalid DeleteCustomerParams", Code = -1 });
        var queryParams = new Dictionary<string, string>
        {
            { "customerId", parameters.CustomerId }
        };
        return await HandleDeleteRequest<DeleteCustomerResponse>("/customer/by-id", queryParams, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Toggles the status (enable/disable) of a customer by ID.
    /// </summary>
    /// <param name="parameters">The parameters for toggling status.</param>
    /// <param name="options">Optional request configurations (e.g. idempotency keys).</param>
    /// <returns>The toggle status response.</returns>
    /// <exception cref="KeyMintApiException">Thrown if the API returns an error.</exception>
    public async Task<KeyMintResult<ToggleCustomerStatusResponse>> ToggleCustomerStatus(ToggleCustomerStatusParams parameters, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (parameters == null || !parameters.IsValid())
            return KeyMintResult<ToggleCustomerStatusResponse>.Failure(new KeyMintApiError { Message = "Invalid ToggleCustomerStatusParams", Code = -1 });
        var fullEndpoint = $"/customer/disable?customerId={parameters.CustomerId}";
        return await HandleRequest<ToggleCustomerStatusResponse>(fullEndpoint, new { }, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves a customer by ID.
    /// </summary>
    /// <param name="parameters">The parameters for retrieval.</param>
    /// <returns>The customer response.</returns>
    /// <exception cref="KeyMintApiException">Thrown if the API returns an error.</exception>
    public async Task<KeyMintResult<GetCustomerByIdResponse>> GetCustomerById(GetCustomerByIdParams parameters, CancellationToken cancellationToken = default)
    {
        if (parameters == null || !parameters.IsValid())
            return KeyMintResult<GetCustomerByIdResponse>.Failure(new KeyMintApiError { Message = "Invalid GetCustomerByIdParams", Code = -1 });
        var queryParams = new Dictionary<string, string>
        {
            { "customerId", parameters.CustomerId }
        };
        return await HandleGetRequest<GetCustomerByIdResponse>("/customer/by-id", queryParams, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Verifies a webhook payload signature received from Keymint.
    /// </summary>
    /// <param name="payload">The raw request body as string.</param>
    /// <param name="header">The value of the "Keymint-Signature" header.</param>
    /// <param name="secret">The webhook endpoint's signing secret.</param>
    /// <param name="toleranceSeconds">Time tolerance in seconds to prevent replay attacks. Defaults to 300 (5 minutes).</param>
    /// <returns>True if signature is valid, false otherwise.</returns>
    public static bool VerifyWebhookSignature(
        string payload,
        string header,
        string secret,
        int toleranceSeconds = 300
    )
    {
        if (string.IsNullOrEmpty(header) || string.IsNullOrEmpty(secret))
        {
            return false;
        }

        try
        {
            // Parse header (e.g. t=1719374021,v1=signature)
            string timestampStr = "";
            string signature = "";
            
            var parts = header.Split(',');
            foreach (var part in parts)
            {
                var kv = part.Trim().Split(new[] { '=' }, 2);
                if (kv.Length == 2)
                {
                    if (kv[0] == "t")
                    {
                        timestampStr = kv[1];
                    }
                    else if (kv[0] == "v1")
                    {
                        signature = kv[1];
                    }
                }
            }

            if (string.IsNullOrEmpty(timestampStr) || string.IsNullOrEmpty(signature))
            {
                return false;
            }

            // Check timestamp validity
            if (!long.TryParse(timestampStr, out long timestampInt))
            {
                return false;
            }

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (Math.Abs(now - timestampInt) > toleranceSeconds)
            {
                return false;
            }

            // Verify HMAC signature
            string signableContent = timestampStr + "." + payload;
            byte[] secretBytes = System.Text.Encoding.UTF8.GetBytes(secret);
            byte[] payloadBytes = System.Text.Encoding.UTF8.GetBytes(signableContent);

            byte[] expectedBytes;
            using (var hmac = new System.Security.Cryptography.HMACSHA256(secretBytes))
            {
                expectedBytes = hmac.ComputeHash(payloadBytes);
            }

            string expectedSignature = BitConverter.ToString(expectedBytes).Replace("-", "").ToLower();

            // Convert client signature from hex string to byte array
            if (signature.Length != expectedSignature.Length)
            {
                return false;
            }

            byte[] signatureBytes = new byte[signature.Length / 2];
            for (int i = 0; i < signatureBytes.Length; i++)
            {
                signatureBytes[i] = Convert.ToByte(signature.Substring(i * 2, 2), 16);
            }

            // Timing-safe verification to prevent timing attacks.
            return FixedTimeEquals(expectedBytes, signatureBytes);
        }
        catch
        {
            return false;
        }
    }

    private static bool FixedTimeEquals(byte[] left, byte[] right)
    {
        if (left == null || right == null || left.Length != right.Length)
        {
            return false;
        }

        int result = 0;
        for (int i = 0; i < left.Length; i++)
        {
            result |= left[i] ^ right[i];
        }

        return result == 0;
    }
}