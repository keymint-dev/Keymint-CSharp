namespace Keymint.CsharpSdk.Services
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents the structure for creating a new customer
    /// when creating a license key.
    /// </summary>
    public class NewCustomer
    {
        [JsonPropertyName("name")]
        public required string Name { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; } // Optional: Email of the new customer
    }

    /// <summary>
    /// Parameters for the createKey API endpoint.
    /// </summary>
    public class CreateKeyParams
    {
        [JsonPropertyName("productId")]
        public required string ProductId { get; set; } // Required: The unique identifier of the product.
        [JsonPropertyName("maxActivations")]
        public string? MaxActivations { get; set; }    // Optional: The maximum number of times the key can be activated.
        [JsonPropertyName("expiryDate")]
        public string? ExpiryDate { get; set; }        // Optional: The expiration date of the key in ISO 8601 format.
        [JsonPropertyName("customerId")]
        public string? CustomerId { get; set; }   // Optional: The ID of an existing customer to associate with the key.
        [JsonPropertyName("newCustomer")]
        public NewCustomer? NewCustomer { get; set; }  // Optional: An object to create and associate a new customer with the key.
    }

    /// <summary>
    /// Response structure for a successful createKey API call.
    /// </summary>
    public class CreateKeyResponse
    {
        [JsonPropertyName("code")]
        public required int Code { get; set; } // API response code (e.g., 0 for success)
        [JsonPropertyName("key")]
        public required string Key { get; set; }  // The generated license key
    }

    /// <summary>
    /// Standard error response structure from the KeyMint API.
    /// </summary>
    public class KeyMintApiError
    {
        [JsonPropertyName("message")]
        public required string Message { get; set; } // Descriptive error message
        [JsonPropertyName("code")]
        public required int Code { get; set; }    // API specific error code
        [JsonPropertyName("status")]
        public int? Status { get; set; }  // HTTP status code, optional
    }

    /// <summary>
    /// Parameters for the activateKey API endpoint.
    /// </summary>
    public class ActivateKeyParams
    {
        [JsonPropertyName("productId")]
        public required string ProductId { get; set; }  // Required: The unique identifier of the product.
        [JsonPropertyName("licenseKey")]
        public required string LicenseKey { get; set; } // Required: The license key to activate.
        [JsonPropertyName("hostId")]
        public string? HostId { get; set; }     // Optional: A unique identifier for the device.
        [JsonPropertyName("deviceTag")]
        public string? DeviceTag { get; set; }  // Optional: A user-friendly name for the device.
    }

    /// <summary>
    /// Response structure for a successful activateKey API call.
    /// </summary>
    public class ActivateKeyResponse
    {
        [JsonPropertyName("code")]
        public required int Code { get; set; }             // API response code (e.g., 0 for success)
        [JsonPropertyName("message")]
        public required string Message { get; set; }          // Activation status message (e.g., "License valid")
        [JsonPropertyName("licenseeName")]
        public string? LicenseeName { get; set; }    // Optional: Name of the licensee (updated field name)
        [JsonPropertyName("licenseeEmail")]
        public string? LicenseeEmail { get; set; }   // Optional: Email of the licensee (updated field name)
    }

    /// <summary>
    /// Parameters for the deactivateKey API endpoint.
    /// </summary>
    public class DeactivateKeyParams
    {
        [JsonPropertyName("productId")]
        public required string ProductId { get; set; }  // Required: The unique identifier of the product.
        [JsonPropertyName("licenseKey")]
        public required string LicenseKey { get; set; } // Required: The license key to deactivate.
        [JsonPropertyName("hostId")]
        public string? HostId { get; set; }     // Optional: The unique identifier of the device to deactivate. If omitted, all devices are deactivated.
    }

    /// <summary>
    /// Response structure for a successful deactivateKey API call.
    /// </summary>
    public class DeactivateKeyResponse
    {
        [JsonPropertyName("message")]
        public required string Message { get; set; } // Confirmation message (e.g., "Device deactivated")
        [JsonPropertyName("code")]
        public required int Code { get; set; }    // API response code (e.g., 0 for success)
    }

    /// <summary>
    /// Device details included in the GetKeyResponse.
    /// </summary>
    public class DeviceDetails
    {
        [JsonPropertyName("hostId")]
        public required string HostId { get; set; }           // Updated field name
        [JsonPropertyName("deviceTag")]
        public string? DeviceTag { get; set; }       // Updated field name  
        [JsonPropertyName("ipAddress")]
        public string? IpAddress { get; set; }       // Updated field name
        [JsonPropertyName("activationTime")]
        public required string ActivationTime { get; set; }   // Updated field name
    }

    /// <summary>
    /// License details included in the GetKeyResponse.
    /// </summary>
    public class LicenseDetails
    {
        [JsonPropertyName("id")]
        public required string Id { get; set; }
        [JsonPropertyName("key")]
        public required string Key { get; set; }
        [JsonPropertyName("productId")]
        public required string ProductId { get; set; }        // Updated field name
        [JsonPropertyName("maxActivations")]
        public required int MaxActivations { get; set; }   // Updated field name
        [JsonPropertyName("activations")]
        public required int Activations { get; set; }
        [JsonPropertyName("devices")]
        public required List<DeviceDetails> Devices { get; set; }
        [JsonPropertyName("activated")]
        public required bool Activated { get; set; }
        [JsonPropertyName("expirationDate")]
        public string? ExpirationDate { get; set; }  // Updated field name
    }

    /// <summary>
    /// Customer details included in the GetKeyResponse.
    /// </summary>
    public class CustomerDetails
    {
        [JsonPropertyName("id")]
        public required string Id { get; set; }
        [JsonPropertyName("name")]
        public string? Name { get; set; } // Optional
        [JsonPropertyName("email")]
        public string? Email { get; set; } // Optional
        [JsonPropertyName("active")]
        public required bool Active { get; set; }
    }

    /// <summary>
    /// Parameters for the getKey API endpoint.
    /// </summary>
    public class GetKeyParams
    {
        [JsonPropertyName("productId")]
        public required string ProductId { get; set; }  // Required: The unique identifier of the product.
        [JsonPropertyName("licenseKey")]
        public required string LicenseKey { get; set; } // Required: The license key to retrieve.
    }

    /// <summary>
    /// Response structure for a successful getKey API call.
    /// </summary>
    public class GetKeyResponse
    {
        [JsonPropertyName("code")]
        public required int Code { get; set; } // API response code (e.g., 0 for success)
        [JsonPropertyName("data")]
        public required GetKeyResponseData Data { get; set; }
    }

    public class GetKeyResponseData
    {
        [JsonPropertyName("license")]
        public required LicenseDetails License { get; set; }
        [JsonPropertyName("customer")]
        public CustomerDetails? Customer { get; set; } // Optional, customer data might not be present
    }

    /// <summary>
    /// Parameters for the blockKey API endpoint.
    /// </summary>
    public class BlockKeyParams
    {
        [JsonPropertyName("productId")]
        public required string ProductId { get; set; }  // Required: The unique identifier of the product.
        [JsonPropertyName("licenseKey")]
        public required string LicenseKey { get; set; } // Required: The license key to block.
    }

    /// <summary>
    /// Response structure for a successful blockKey API call.
    /// </summary>
    public class BlockKeyResponse
    {
        [JsonPropertyName("message")]
        public required string Message { get; set; } // Confirmation message (e.g., "Key blocked")
        [JsonPropertyName("code")]
        public required int Code { get; set; }    // API response code (e.g., 0 for success)
    }

    /// <summary>
    /// Parameters for the unblockKey API endpoint.
    /// </summary>
    public class UnblockKeyParams
    {
        [JsonPropertyName("productId")]
        public required string ProductId { get; set; }  // Required: The unique identifier of the product.
        [JsonPropertyName("licenseKey")]
        public required string LicenseKey { get; set; } // Required: The license key to unblock.
    }

    /// <summary>
    /// Response structure for a successful unblockKey API call.
    /// </summary>
    public class UnblockKeyResponse
    {
        [JsonPropertyName("message")]
        public required string Message { get; set; } // Confirmation message (e.g., "Key unblocked")
        [JsonPropertyName("code")]
        public required int Code { get; set; }    // API response code (e.g., 0 for success)
    }

    /// <summary>
    /// Parameters for the createCustomer API endpoint.
    /// </summary>
    public class CreateCustomerParams
    {
        [JsonPropertyName("name")]
        public required string Name { get; set; }     // Required: Customer name
        [JsonPropertyName("email")]
        public required string Email { get; set; }    // Required: Customer email
    }

    /// <summary>
    /// Response structure for a successful createCustomer API call.
    /// </summary>
    public class CreateCustomerResponse
    {
        [JsonPropertyName("action")]
        public required string Action { get; set; }   // Action performed (e.g., "createCustomer")
        [JsonPropertyName("status")]
        public required bool Status { get; set; }  // Success status
        [JsonPropertyName("message")]
        public required string Message { get; set; }  // Success message
        [JsonPropertyName("data")]
        public required CustomerData Data { get; set; }
        [JsonPropertyName("code")]
        public required int Code { get; set; }     // API response code (e.g., 0 for success)
    }

    public class CustomerData
    {
        [JsonPropertyName("id")]
        public required string Id { get; set; }     // Customer ID
        [JsonPropertyName("name")]
        public required string Name { get; set; }   // Customer name
        [JsonPropertyName("email")]
        public required string Email { get; set; }  // Customer email
    }

    /// <summary>
    /// Customer information in the getAllCustomers response.
    /// </summary>
    public class Customer
    {
        [JsonPropertyName("id")]
        public required string Id { get; set; }
        [JsonPropertyName("name")]
        public required string Name { get; set; }
        [JsonPropertyName("email")]
        public required string Email { get; set; }
        [JsonPropertyName("active")]
        public required bool Active { get; set; }
        [JsonPropertyName("createdAt")]
        public required string CreatedAt { get; set; }
        [JsonPropertyName("updatedAt")]
        public required string UpdatedAt { get; set; }
        [JsonPropertyName("createdBy")]
        public required string CreatedBy { get; set; }
    }

    /// <summary>
    /// Response structure for a successful getAllCustomers API call.
    /// </summary>
    public class GetAllCustomersResponse
    {
        [JsonPropertyName("action")]
        public required string Action { get; set; }     // Action performed (e.g., "getCustomers")
        [JsonPropertyName("status")]
        public required bool Status { get; set; }    // Success status
        [JsonPropertyName("data")]
        public required List<Customer> Data { get; set; }   // Array of customer objects
        [JsonPropertyName("code")]
        public required int Code { get; set; }       // API response code (e.g., 0 for success)
    }

    /// <summary>
    /// Parameters for the getCustomerWithKeys API endpoint.
    /// </summary>
    public class GetCustomerWithKeysParams
    {
        [JsonPropertyName("customerId")]
        public required string CustomerId { get; set; } // Required: The customer ID
    }

    /// <summary>
    /// License key information in customer with keys response.
    /// </summary>
    public class CustomerLicenseKey
    {
        [JsonPropertyName("id")]
        public required string Id { get; set; }
        [JsonPropertyName("key")]
        public required string Key { get; set; }
        [JsonPropertyName("productId")]
        public required string ProductId { get; set; }
        [JsonPropertyName("maxActivations")]
        public required int MaxActivations { get; set; }
        [JsonPropertyName("activations")]
        public required int Activations { get; set; }
        [JsonPropertyName("activated")]
        public required bool Activated { get; set; }
        [JsonPropertyName("expirationDate")]
        public string? ExpirationDate { get; set; }
    }

    /// <summary>
    /// Response structure for a successful getCustomerWithKeys API call.
    /// </summary>
    public class GetCustomerWithKeysResponse
    {
        [JsonPropertyName("action")]
        public required string Action { get; set; }
        [JsonPropertyName("status")]
        public required bool Status { get; set; }
        [JsonPropertyName("data")]
        public required CustomerWithKeysData Data { get; set; }
        [JsonPropertyName("code")]
        public required int Code { get; set; }
    }

    public class CustomerWithKeysData
    {
        [JsonPropertyName("customer")]
        public required Customer Customer { get; set; }
        [JsonPropertyName("licenseKeys")]
        public required List<CustomerLicenseKey> LicenseKeys { get; set; }
    }

    /// <summary>
    /// Parameters for the updateCustomer API endpoint.
    /// </summary>
    public class UpdateCustomerParams
    {
        [JsonPropertyName("customerId")]
        public required string CustomerId { get; set; }  // Required: The customer ID
        [JsonPropertyName("name")]
        public string? Name { get; set; }       // Optional: Updated customer name
        [JsonPropertyName("email")]
        public string? Email { get; set; }      // Optional: Updated customer email
        [JsonPropertyName("active")]
        public bool? Active { get; set; }    // Optional: Customer active status
    }

    /// <summary>
    /// Response structure for a successful updateCustomer API call.
    /// </summary>
    public class UpdateCustomerResponse
    {
        [JsonPropertyName("action")]
        public required string Action { get; set; }
        [JsonPropertyName("status")]
        public required bool Status { get; set; }
        [JsonPropertyName("message")]
        public required string Message { get; set; }
        [JsonPropertyName("data")]
        public required Customer Data { get; set; }
        [JsonPropertyName("code")]
        public required int Code { get; set; }
    }

    /// <summary>
    /// Parameters for the toggleCustomerStatus API endpoint.
    /// </summary>
    public class ToggleCustomerStatusParams
    {
        [JsonPropertyName("customerId")]
        public required string CustomerId { get; set; }  // Required: The customer ID
    }

    /// <summary>
    /// Response structure for a successful toggleCustomerStatus API call.
    /// </summary>
    public class ToggleCustomerStatusResponse
    {
        [JsonPropertyName("action")]
        public required string Action { get; set; }      // Action performed (e.g., "toggleActive")
        [JsonPropertyName("status")]
        public required bool Status { get; set; }     // Success status
        [JsonPropertyName("message")]
        public required string Message { get; set; }     // Status message (e.g., "Customer disabled")
        [JsonPropertyName("code")]
        public required int Code { get; set; }        // API response code
    }

    /// <summary>
    /// Parameters for the getCustomerById API endpoint.
    /// </summary>
    public class GetCustomerByIdParams
    {
        [JsonPropertyName("customerId")]
        public required string CustomerId { get; set; }  // Required: The customer ID
    }

    /// <summary>
    /// Response structure for a successful getCustomerById API call.
    /// </summary>
    public class GetCustomerByIdResponse
    {
        [JsonPropertyName("action")]
        public required string Action { get; set; }      // Action performed (e.g., "getCustomerById")
        [JsonPropertyName("status")]
        public required bool Status { get; set; }     // Success status
        [JsonPropertyName("data")]
        public required List<Customer> Data { get; set; }    // Array containing the customer object
        [JsonPropertyName("code")]
        public required int Code { get; set; }        // API response code
    }

    /// <summary>
    /// Parameters for the deleteCustomer API endpoint.
    /// </summary>
    public class DeleteCustomerParams
    {
        [JsonPropertyName("customerId")]
        public required string CustomerId { get; set; }  // Required: The customer ID
    }

    /// <summary>
    /// Response structure for a successful deleteCustomer API call.
    /// </summary>
    public class DeleteCustomerResponse
    {
        [JsonPropertyName("action")]
        public required string Action { get; set; }      // Action performed (e.g., "deleteCustomer")
        [JsonPropertyName("status")]
        public required bool Status { get; set; }     // Success status
        [JsonPropertyName("message")]
        public required string Message { get; set; }     // Status message (e.g., "Customer deleted")
        [JsonPropertyName("code")]
        public required int Code { get; set; }        // API response code
    }
}