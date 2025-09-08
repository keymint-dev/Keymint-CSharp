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

    public KeyMintSDK(string accessToken, string baseUrl = "https://api.keymint.dev", ILogger<KeyMintSDK>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(accessToken, nameof(accessToken));

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl)
        };

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        _logger = logger;
    }

    /// <summary>
    /// Generic method to handle POST requests
    /// </summary>
    private async Task<KeyMintResult<T>> HandleRequest<T>(string endpoint, object parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(endpoint, parameters, cancellationToken).ConfigureAwait(false);
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
    private async Task<KeyMintResult<T>> HandleDeleteRequest<T>(string endpoint, Dictionary<string, string>? queryParams = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var queryString = queryParams != null ? ToQueryString(queryParams) : string.Empty;
            var fullEndpoint = string.IsNullOrEmpty(queryString) ? endpoint : $"{endpoint}?{queryString}";
            var response = await _httpClient.DeleteAsync(fullEndpoint, cancellationToken).ConfigureAwait(false);
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
    private async Task<KeyMintResult<T>> HandlePutRequest<T>(string endpoint, object parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync(endpoint, parameters, cancellationToken).ConfigureAwait(false);
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
    /// <returns>The created key response or error.</returns>
    public async Task<KeyMintResult<CreateKeyResponse>> CreateKey(CreateKeyParams parameters, CancellationToken cancellationToken = default)
    {
        if (parameters == null || !parameters.IsValid())
            return KeyMintResult<CreateKeyResponse>.Failure(new KeyMintApiError { Message = "Invalid CreateKeyParams", Code = -1 });
        var response = await _httpClient.PostAsJsonAsync("/key", parameters, cancellationToken).ConfigureAwait(false);
        return await HandleResponse<CreateKeyResponse>(response).ConfigureAwait(false);
    }

    /// <summary>
    /// Activates a license key.
    /// </summary>
    /// <param name="parameters">The parameters for key activation.</param>
    /// <returns>The activation response or error.</returns>
    public async Task<KeyMintResult<ActivateKeyResponse>> ActivateKey(ActivateKeyParams parameters, CancellationToken cancellationToken = default)
    {
        if (parameters == null || !parameters.IsValid())
            return KeyMintResult<ActivateKeyResponse>.Failure(new KeyMintApiError { Message = "Invalid ActivateKeyParams", Code = -1 });
        var response = await _httpClient.PostAsJsonAsync("/key/activate", parameters, cancellationToken).ConfigureAwait(false);
        return await HandleResponse<ActivateKeyResponse>(response).ConfigureAwait(false);
    }

    /// <summary>
    /// Deactivates a license key.
    /// </summary>
    /// <param name="parameters">The parameters for key deactivation.</param>
    /// <returns>The deactivation response.</returns>
    /// <exception cref="KeyMintApiException">Thrown if the API returns an error.</exception>
    public async Task<KeyMintResult<DeactivateKeyResponse>> DeactivateKey(DeactivateKeyParams parameters, CancellationToken cancellationToken = default)
    {
        if (parameters == null || !parameters.IsValid())
            return KeyMintResult<DeactivateKeyResponse>.Failure(new KeyMintApiError { Message = "Invalid DeactivateKeyParams", Code = -1 });
        var response = await _httpClient.PostAsJsonAsync("/key/deactivate", parameters, cancellationToken).ConfigureAwait(false);
        return await HandleResponse<DeactivateKeyResponse>(response).ConfigureAwait(false);
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
    /// <returns>The block response.</returns>
    /// <exception cref="KeyMintApiException">Thrown if the API returns an error.</exception>
    public async Task<KeyMintResult<BlockKeyResponse>> BlockKey(BlockKeyParams parameters, CancellationToken cancellationToken = default)
    {
        if (parameters == null || !parameters.IsValid())
            return KeyMintResult<BlockKeyResponse>.Failure(new KeyMintApiError { Message = "Invalid BlockKeyParams", Code = -1 });
        var response = await _httpClient.PostAsJsonAsync("/key/block", parameters, cancellationToken).ConfigureAwait(false);
        return await HandleResponse<BlockKeyResponse>(response).ConfigureAwait(false);
    }

    /// <summary>
    /// Unblocks a license key.
    /// </summary>
    /// <param name="parameters">The parameters for unblocking the key.</param>
    /// <returns>The unblock response.</returns>
    /// <exception cref="KeyMintApiException">Thrown if the API returns an error.</exception>
    public async Task<KeyMintResult<UnblockKeyResponse>> UnblockKey(UnblockKeyParams parameters, CancellationToken cancellationToken = default)
    {
        if (parameters == null || !parameters.IsValid())
            return KeyMintResult<UnblockKeyResponse>.Failure(new KeyMintApiError { Message = "Invalid UnblockKeyParams", Code = -1 });
        var response = await _httpClient.PostAsJsonAsync("/key/unblock", parameters, cancellationToken).ConfigureAwait(false);
        return await HandleResponse<UnblockKeyResponse>(response).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a new customer.
    /// </summary>
    /// <param name="parameters">The parameters for customer creation.</param>
    /// <returns>The created customer response.</returns>
    /// <exception cref="KeyMintApiException">Thrown if the API returns an error.</exception>
    public async Task<KeyMintResult<CreateCustomerResponse>> CreateCustomer(CreateCustomerParams parameters, CancellationToken cancellationToken = default)
    {
        if (parameters == null || !parameters.IsValid())
            return KeyMintResult<CreateCustomerResponse>.Failure(new KeyMintApiError { Message = "Invalid CreateCustomerParams", Code = -1 });
        return await HandleRequest<CreateCustomerResponse>("/customer", parameters, cancellationToken).ConfigureAwait(false);
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
    /// <returns>The update response.</returns>
    /// <exception cref="KeyMintApiException">Thrown if the API returns an error.</exception>
    public async Task<KeyMintResult<UpdateCustomerResponse>> UpdateCustomer(UpdateCustomerParams parameters, CancellationToken cancellationToken = default)
    {
        if (parameters == null || !parameters.IsValid())
            return KeyMintResult<UpdateCustomerResponse>.Failure(new KeyMintApiError { Message = "Invalid UpdateCustomerParams", Code = -1 });
        return await HandlePutRequest<UpdateCustomerResponse>("/customer/by-id", parameters, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes a customer by ID.
    /// </summary>
    /// <param name="parameters">The parameters for deletion.</param>
    /// <returns>The delete response.</returns>
    /// <exception cref="KeyMintApiException">Thrown if the API returns an error.</exception>
    public async Task<KeyMintResult<DeleteCustomerResponse>> DeleteCustomer(DeleteCustomerParams parameters, CancellationToken cancellationToken = default)
    {
        if (parameters == null || !parameters.IsValid())
            return KeyMintResult<DeleteCustomerResponse>.Failure(new KeyMintApiError { Message = "Invalid DeleteCustomerParams", Code = -1 });
        var fullEndpoint = $"/customer/by-id?customerId={parameters.CustomerId}";
        var response = await _httpClient.DeleteAsync(fullEndpoint, cancellationToken).ConfigureAwait(false);
        return await HandleResponse<DeleteCustomerResponse>(response).ConfigureAwait(false);
    }

    /// <summary>
    /// Toggles the status (enable/disable) of a customer by ID.
    /// </summary>
    /// <param name="parameters">The parameters for toggling status.</param>
    /// <returns>The toggle status response.</returns>
    /// <exception cref="KeyMintApiException">Thrown if the API returns an error.</exception>
    public async Task<KeyMintResult<ToggleCustomerStatusResponse>> ToggleCustomerStatus(ToggleCustomerStatusParams parameters, CancellationToken cancellationToken = default)
    {
        if (parameters == null || !parameters.IsValid())
            return KeyMintResult<ToggleCustomerStatusResponse>.Failure(new KeyMintApiError { Message = "Invalid ToggleCustomerStatusParams", Code = -1 });
        var fullEndpoint = $"/customer/disable?customerId={parameters.CustomerId}";
        var response = await _httpClient.PostAsJsonAsync(fullEndpoint, new { }, cancellationToken).ConfigureAwait(false);
        return await HandleResponse<ToggleCustomerStatusResponse>(response).ConfigureAwait(false);
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
}