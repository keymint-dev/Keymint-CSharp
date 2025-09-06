using System;
using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Web;
using Csharp_Sdk.Services;

namespace Csharp_Sdk;

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

        var content = await response.Content.ReadFromJsonAsync<T>();
        if (content == null)
        {
            throw new KeyMintApiException("Failed to deserialize API response", -1, response.StatusCode);
        }

        return content;
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
        return query.ToString() ?? "";
    }

    // API Methods

    public async Task<CreateKeyResponse> CreateKey(CreateKeyParams parameters)
    {
        return await HandleRequest<CreateKeyResponse>("/key", parameters);
    }

    public async Task<ActivateKeyResponse> ActivateKey(ActivateKeyParams parameters)
    {
        return await HandleRequest<ActivateKeyResponse>("/key/activate", parameters);
    }

    public async Task<DeactivateKeyResponse> DeactivateKey(DeactivateKeyParams parameters)
    {
        return await HandleRequest<DeactivateKeyResponse>("/key/deactivate", parameters);
    }

    public async Task<GetKeyResponse> GetKey(GetKeyParams parameters)
    {
        var queryParams = new Dictionary<string, string>
        {
            { "productId", parameters.ProductId },
            { "licenseKey", parameters.LicenseKey }
        };
        return await HandleGetRequest<GetKeyResponse>("/key", queryParams);
    }

    public async Task<BlockKeyResponse> BlockKey(BlockKeyParams parameters)
    {
        return await HandleRequest<BlockKeyResponse>("/key/block", parameters);
    }

    public async Task<UnblockKeyResponse> UnblockKey(UnblockKeyParams parameters)
    {
        return await HandleRequest<UnblockKeyResponse>("/key/unblock", parameters);
    }

    public async Task<CreateCustomerResponse> CreateCustomer(CreateCustomerParams parameters)
    {
        return await HandleRequest<CreateCustomerResponse>("/customer", parameters);
    }

    public async Task<GetAllCustomersResponse> GetAllCustomers()
    {
        return await HandleGetRequest<GetAllCustomersResponse>("/customer");
    }

    public async Task<GetCustomerWithKeysResponse> GetCustomerWithKeys(GetCustomerWithKeysParams parameters)
    {
        var queryParams = new Dictionary<string, string>
        {
            { "customerId", parameters.CustomerId }
        };
        return await HandleGetRequest<GetCustomerWithKeysResponse>("/customer/keys", queryParams);
    }

    public async Task<UpdateCustomerResponse> UpdateCustomer(UpdateCustomerParams parameters)
    {
        return await HandlePutRequest<UpdateCustomerResponse>("/customer/by-id", parameters);
    }

    public async Task<DeleteCustomerResponse> DeleteCustomer(DeleteCustomerParams parameters)
    {
        var queryParams = new Dictionary<string, string>
        {
            { "customerId", parameters.CustomerId }
        };
        return await HandleDeleteRequest<DeleteCustomerResponse>("/customer/by-id", queryParams);
    }

    public async Task<ToggleCustomerStatusResponse> ToggleCustomerStatus(ToggleCustomerStatusParams parameters)
    {
        var requestParams = new { customerId = parameters.CustomerId };
        return await HandleRequest<ToggleCustomerStatusResponse>("/customer/disable", requestParams);
    }

    public async Task<GetCustomerByIdResponse> GetCustomerById(GetCustomerByIdParams parameters)
    {
        var queryParams = new Dictionary<string, string>
        {
            { "customerId", parameters.CustomerId }
        };
        return await HandleGetRequest<GetCustomerByIdResponse>("/customer/by-id", queryParams);
    }
}