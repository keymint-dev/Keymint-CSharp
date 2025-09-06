using System;
using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Web;
using Microsoft.Extensions.Logging;


namespace Keymint.CsharpSdk.Services;

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

    public KeyMintSDK(string accessToken, string baseUrl = "https://api.keymint.dev")
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
    }

    /// <summary>
    /// Generic method to handle POST requests
    /// </summary>
    private async Task<T> HandleRequest<T>(string endpoint, object parameters)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(endpoint, parameters);
            return await HandleResponse<T>(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex.Message);
            throw HandleError(ex);
        }
    }

    /// <summary>
    /// Generic method to handle GET requests
    /// </summary>
    private async Task<T> HandleGetRequest<T>(string endpoint, Dictionary<string, string>? queryParams = null)
    {
        try
        {
            var queryString = queryParams != null ? ToQueryString(queryParams) : string.Empty;
            var fullEndpoint = string.IsNullOrEmpty(queryString) ? endpoint : $"{endpoint}?{queryString}";
            
            var response = await _httpClient.GetAsync(fullEndpoint);
            return await HandleResponse<T>(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex.Message);
            throw HandleError(ex);
        }
    }

    /// <summary>
    /// Generic method to handle DELETE requests
    /// </summary>
    private async Task<T> HandleDeleteRequest<T>(string endpoint, Dictionary<string, string>? queryParams = null)
    {
        try
        {
            var queryString = queryParams != null ? ToQueryString(queryParams) : string.Empty;
            var fullEndpoint = string.IsNullOrEmpty(queryString) ? endpoint : $"{endpoint}?{queryString}";
            
            var response = await _httpClient.DeleteAsync(fullEndpoint);
            return await HandleResponse<T>(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex.Message);
            throw HandleError(ex);
        }
    }

    /// <summary>
    /// Generic method to handle PUT requests
    /// </summary>
    private async Task<T> HandlePutRequest<T>(string endpoint, object parameters)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync(endpoint, parameters);
            return await HandleResponse<T>(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex.Message);
            throw HandleError(ex);
        }
    }

    /// <summary>
    /// Handles HTTP response and deserializes the content
    /// </summary>
    private async Task<T> HandleResponse<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            try
            {
                var errorContent = await response.Content.ReadFromJsonAsync<KeyMintApiError>();
                if (errorContent != null)
                {
                    throw new KeyMintApiException(errorContent.Message, errorContent.Code, response.StatusCode)
                    {
                        ApiError = errorContent
                    };
                }
            }
            catch
            {
                // Fallback if we can't parse the error response
                throw new KeyMintApiException($"HTTP error: {response.StatusCode} - {response.ReasonPhrase}", -1, response.StatusCode);
            }
            throw new KeyMintApiException($"HTTP error: {response.StatusCode} - {response.ReasonPhrase}", -1, response.StatusCode);
        }

        var rawJson = await response.Content.ReadAsStringAsync();
        try
        {
            var content = System.Text.Json.JsonSerializer.Deserialize<T>(rawJson);
            if (content == null)
            {
                throw new KeyMintApiException("Failed to deserialize API response", -1, response.StatusCode);
            }
            return content;
        }
        catch (System.Text.Json.JsonException jsonEx)
        {
            Console.Error.WriteLine($"[DEBUG] Failed to deserialize response to {typeof(T)}. Raw JSON:\n{rawJson}");
            throw new KeyMintApiException($"Deserialization error: {jsonEx.Message}", -1, response.StatusCode, jsonEx);
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
    /// <returns>The created key response.</returns>
    /// <exception cref="KeyMintApiException">Thrown if the API returns an error.</exception>
    public async Task<CreateKeyResponse> CreateKey(CreateKeyParams parameters)
    {
        var response = await _httpClient.PostAsJsonAsync("/key", parameters);
        return await HandleResponse<CreateKeyResponse>(response);
    }

    /// <summary>
    /// Activates a license key.
    /// </summary>
    /// <param name="parameters">The parameters for key activation.</param>
    /// <returns>The activation response.</returns>
    /// <exception cref="KeyMintApiException">Thrown if the API returns an error.</exception>
    public async Task<ActivateKeyResponse> ActivateKey(ActivateKeyParams parameters)
    {
        var response = await _httpClient.PostAsJsonAsync("/key/activate", parameters);
        return await HandleResponse<ActivateKeyResponse>(response);
    }

    /// <summary>
    /// Deactivates a license key.
    /// </summary>
    /// <param name="parameters">The parameters for key deactivation.</param>
    /// <returns>The deactivation response.</returns>
    /// <exception cref="KeyMintApiException">Thrown if the API returns an error.</exception>
    public async Task<DeactivateKeyResponse> DeactivateKey(DeactivateKeyParams parameters)
    {
        var response = await _httpClient.PostAsJsonAsync("/key/deactivate", parameters);
        return await HandleResponse<DeactivateKeyResponse>(response);
    }

    /// <summary>
    /// Retrieves a license key by product and license key.
    /// </summary>
    /// <param name="parameters">The parameters for key retrieval.</param>
    /// <returns>The key response.</returns>
    /// <exception cref="KeyMintApiException">Thrown if the API returns an error.</exception>
    public async Task<GetKeyResponse> GetKey(GetKeyParams parameters)
    {
        var fullEndpoint = $"/key?productId={parameters.ProductId}&licenseKey={parameters.LicenseKey}";
        var response = await _httpClient.GetAsync(fullEndpoint);
        return await HandleResponse<GetKeyResponse>(response);
    }

    /// <summary>
    /// Blocks a license key.
    /// </summary>
    /// <param name="parameters">The parameters for blocking the key.</param>
    /// <returns>The block response.</returns>
    /// <exception cref="KeyMintApiException">Thrown if the API returns an error.</exception>
    public async Task<BlockKeyResponse> BlockKey(BlockKeyParams parameters)
    {
        var response = await _httpClient.PostAsJsonAsync("/key/block", parameters);
        return await HandleResponse<BlockKeyResponse>(response);
    }

    /// <summary>
    /// Unblocks a license key.
    /// </summary>
    /// <param name="parameters">The parameters for unblocking the key.</param>
    /// <returns>The unblock response.</returns>
    /// <exception cref="KeyMintApiException">Thrown if the API returns an error.</exception>
    public async Task<UnblockKeyResponse> UnblockKey(UnblockKeyParams parameters)
    {
        var response = await _httpClient.PostAsJsonAsync("/key/unblock", parameters);
        return await HandleResponse<UnblockKeyResponse>(response);
    }

    /// <summary>
    /// Creates a new customer.
    /// </summary>
    /// <param name="parameters">The parameters for customer creation.</param>
    /// <returns>The created customer response.</returns>
    /// <exception cref="KeyMintApiException">Thrown if the API returns an error.</exception>
    public async Task<CreateCustomerResponse> CreateCustomer(CreateCustomerParams parameters)
    {
        return await HandleRequest<CreateCustomerResponse>("/customer", parameters);
    }

    /// <summary>
    /// Retrieves all customers.
    /// </summary>
    /// <returns>The response containing all customers.</returns>
    /// <exception cref="KeyMintApiException">Thrown if the API returns an error.</exception>
    public async Task<GetAllCustomersResponse> GetAllCustomers()
    {
        return await HandleGetRequest<GetAllCustomersResponse>("/customer");
    }

    /// <summary>
    /// Retrieves a customer and their keys by customer ID.
    /// </summary>
    /// <param name="parameters">The parameters for retrieval.</param>
    /// <returns>The customer with keys response.</returns>
    /// <exception cref="KeyMintApiException">Thrown if the API returns an error.</exception>
    public async Task<GetCustomerWithKeysResponse> GetCustomerWithKeys(GetCustomerWithKeysParams parameters)
    {
        var queryParams = new Dictionary<string, string>
        {
            { "customerId", parameters.CustomerId }
        };
        return await HandleGetRequest<GetCustomerWithKeysResponse>("/customer/keys", queryParams);
    }

    /// <summary>
    /// Updates a customer by ID.
    /// </summary>
    /// <param name="parameters">The parameters for updating the customer.</param>
    /// <returns>The update response.</returns>
    /// <exception cref="KeyMintApiException">Thrown if the API returns an error.</exception>
    public async Task<UpdateCustomerResponse> UpdateCustomer(UpdateCustomerParams parameters)
    {
        return await HandlePutRequest<UpdateCustomerResponse>("/customer/by-id", parameters);
    }

    /// <summary>
    /// Deletes a customer by ID.
    /// </summary>
    /// <param name="parameters">The parameters for deletion.</param>
    /// <returns>The delete response.</returns>
    /// <exception cref="KeyMintApiException">Thrown if the API returns an error.</exception>
    public async Task<DeleteCustomerResponse> DeleteCustomer(DeleteCustomerParams parameters)
    {
        var fullEndpoint = $"/customer/by-id?customerId={parameters.CustomerId}";
        var response = await _httpClient.DeleteAsync(fullEndpoint);
        return await HandleResponse<DeleteCustomerResponse>(response);
    }

    /// <summary>
    /// Toggles the status (enable/disable) of a customer by ID.
    /// </summary>
    /// <param name="parameters">The parameters for toggling status.</param>
    /// <returns>The toggle status response.</returns>
    /// <exception cref="KeyMintApiException">Thrown if the API returns an error.</exception>
    public async Task<ToggleCustomerStatusResponse> ToggleCustomerStatus(ToggleCustomerStatusParams parameters)
    {
        var fullEndpoint = $"/customer/disable?customerId={parameters.CustomerId}";
        try
        {
            var response = await _httpClient.PostAsJsonAsync(fullEndpoint, new { });
            return await HandleResponse<ToggleCustomerStatusResponse>(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex.Message);
            throw HandleError(ex);
        }
    }

    /// <summary>
    /// Retrieves a customer by ID.
    /// </summary>
    /// <param name="parameters">The parameters for retrieval.</param>
    /// <returns>The customer response.</returns>
    /// <exception cref="KeyMintApiException">Thrown if the API returns an error.</exception>
    public async Task<GetCustomerByIdResponse> GetCustomerById(GetCustomerByIdParams parameters)
    {
        var queryParams = new Dictionary<string, string>
        {
            { "customerId", parameters.CustomerId }
        };
        return await HandleGetRequest<GetCustomerByIdResponse>("/customer/by-id", queryParams);
    }
}