namespace Csharp_Sdk.Services
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents the structure for creating a new customer
    /// when creating a license key.
    /// </summary>
    public class NewCustomer
    {
        public required string Name { get; set; }
        public string? Email { get; set; } // Optional: Email of the new customer
    }

    /// <summary>
    /// Parameters for the createKey API endpoint.
    /// </summary>
    public class CreateKeyParams
    {
        public required string ProductId { get; set; } // Required: The unique identifier of the product.
        public string? MaxActivations { get; set; }    // Optional: The maximum number of times the key can be activated.
        public string? ExpiryDate { get; set; }        // Optional: The expiration date of the key in ISO 8601 format.
        public string? CustomerId { get; set; }        // Optional: The ID of an existing customer to associate with the key.
        public NewCustomer? NewCustomer { get; set; }  // Optional: An object to create and associate a new customer with the key.
    }

    /// <summary>
    /// Response structure for a successful createKey API call.
    /// </summary>
    public class CreateKeyResponse
    {
        public required int Code { get; set; } // API response code (e.g., 0 for success)
        public required string Key { get; set; }  // The generated license key
    }

    /// <summary>
    /// Standard error response structure from the KeyMint API.
    /// </summary>
    public class KeyMintApiError
    {
        public required string Message { get; set; } // Descriptive error message
        public required int Code { get; set; }    // API specific error code
        public int? Status { get; set; }  // HTTP status code, optional
    }

    /// <summary>
    /// Parameters for the activateKey API endpoint.
    /// </summary>
    public class ActivateKeyParams
    {
        public required string ProductId { get; set; }  // Required: The unique identifier of the product.
        public required string LicenseKey { get; set; } // Required: The license key to activate.
        public string? HostId { get; set; }     // Optional: A unique identifier for the device.
        public string? DeviceTag { get; set; }  // Optional: A user-friendly name for the device.
    }

    /// <summary>
    /// Response structure for a successful activateKey API call.
    /// </summary>
    public class ActivateKeyResponse
    {
        public required int Code { get; set; }             // API response code (e.g., 0 for success)
        public required string Message { get; set; }          // Activation status message (e.g., "License valid")
        public string? LicenseeName { get; set; }    // Optional: Name of the licensee (updated field name)
        public string? LicenseeEmail { get; set; }   // Optional: Email of the licensee (updated field name)
    }

    /// <summary>
    /// Parameters for the deactivateKey API endpoint.
    /// </summary>
    public class DeactivateKeyParams
    {
        public required string ProductId { get; set; }  // Required: The unique identifier of the product.
        public required string LicenseKey { get; set; } // Required: The license key to deactivate.
        public string? HostId { get; set; }     // Optional: The unique identifier of the device to deactivate. If omitted, all devices are deactivated.
    }

    /// <summary>
    /// Response structure for a successful deactivateKey API call.
    /// </summary>
    public class DeactivateKeyResponse
    {
        public required string Message { get; set; } // Confirmation message (e.g., "Device deactivated")
        public required int Code { get; set; }    // API response code (e.g., 0 for success)
    }

    /// <summary>
    /// Device details included in the GetKeyResponse.
    /// </summary>
    public class DeviceDetails
    {
        public required string HostId { get; set; }           // Updated field name
        public string? DeviceTag { get; set; }       // Updated field name  
        public string? IpAddress { get; set; }       // Updated field name
        public required string ActivationTime { get; set; }   // Updated field name
    }

    /// <summary>
    /// License details included in the GetKeyResponse.
    /// </summary>
    public class LicenseDetails
    {
        public required string Id { get; set; }
        public required string Key { get; set; }
        public required string ProductId { get; set; }        // Updated field name
        public required int MaxActivations { get; set; }   // Updated field name
        public required int Activations { get; set; }
        public required List<DeviceDetails> Devices { get; set; }
        public required bool Activated { get; set; }
        public string? ExpirationDate { get; set; }  // Updated field name
    }

    /// <summary>
    /// Customer details included in the GetKeyResponse.
    /// </summary>
    public class CustomerDetails
    {
        public required string Id { get; set; }
        public string? Name { get; set; } // Optional
        public string? Email { get; set; } // Optional
        public required bool Active { get; set; }
    }

    /// <summary>
    /// Parameters for the getKey API endpoint.
    /// </summary>
    public class GetKeyParams
    {
        public required string ProductId { get; set; }  // Required: The unique identifier of the product.
        public required string LicenseKey { get; set; } // Required: The license key to retrieve.
    }

    /// <summary>
    /// Response structure for a successful getKey API call.
    /// </summary>
    public class GetKeyResponse
    {
        public required int Code { get; set; } // API response code (e.g., 0 for success)
        public required GetKeyResponseData Data { get; set; }
    }

    public class GetKeyResponseData
    {
        public required LicenseDetails License { get; set; }
        public CustomerDetails? Customer { get; set; } // Optional, customer data might not be present
    }

    /// <summary>
    /// Parameters for the blockKey API endpoint.
    /// </summary>
    public class BlockKeyParams
    {
        public required string ProductId { get; set; }  // Required: The unique identifier of the product.
        public required string LicenseKey { get; set; } // Required: The license key to block.
    }

    /// <summary>
    /// Response structure for a successful blockKey API call.
    /// </summary>
    public class BlockKeyResponse
    {
        public required string Message { get; set; } // Confirmation message (e.g., "Key blocked")
        public required int Code { get; set; }    // API response code (e.g., 0 for success)
    }

    /// <summary>
    /// Parameters for the unblockKey API endpoint.
    /// </summary>
    public class UnblockKeyParams
    {
        public required string ProductId { get; set; }  // Required: The unique identifier of the product.
        public required string LicenseKey { get; set; } // Required: The license key to unblock.
    }

    /// <summary>
    /// Response structure for a successful unblockKey API call.
    /// </summary>
    public class UnblockKeyResponse
    {
        public required string Message { get; set; } // Confirmation message (e.g., "Key unblocked")
        public required int Code { get; set; }    // API response code (e.g., 0 for success)
    }

    /// <summary>
    /// Parameters for the createCustomer API endpoint.
    /// </summary>
    public class CreateCustomerParams
    {
        public required string Name { get; set; }     // Required: Customer name
        public required string Email { get; set; }    // Required: Customer email
    }

    /// <summary>
    /// Response structure for a successful createCustomer API call.
    /// </summary>
    public class CreateCustomerResponse
    {
        public required string Action { get; set; }   // Action performed (e.g., "createCustomer")
        public required bool Status { get; set; }  // Success status
        public required string Message { get; set; }  // Success message
        public required CustomerData Data { get; set; }
        public required int Code { get; set; }     // API response code (e.g., 0 for success)
    }

    public class CustomerData
    {
        public required string Id { get; set; }     // Customer ID
        public required string Name { get; set; }   // Customer name
        public required string Email { get; set; }  // Customer email
    }

    /// <summary>
    /// Customer information in the getAllCustomers response.
    /// </summary>
    public class Customer
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required bool Active { get; set; }
        public required string CreatedAt { get; set; }
        public required string UpdatedAt { get; set; }
        public required string CreatedBy { get; set; }
    }

    /// <summary>
    /// Response structure for a successful getAllCustomers API call.
    /// </summary>
    public class GetAllCustomersResponse
    {
        public required string Action { get; set; }     // Action performed (e.g., "getCustomers")
        public required bool Status { get; set; }    // Success status
        public required List<Customer> Data { get; set; }   // Array of customer objects
        public required int Code { get; set; }       // API response code (e.g., 0 for success)
    }

    /// <summary>
    /// Parameters for the getCustomerWithKeys API endpoint.
    /// </summary>
    public class GetCustomerWithKeysParams
    {
        public required string CustomerId { get; set; } // Required: The customer ID
    }

    /// <summary>
    /// License key information in customer with keys response.
    /// </summary>
    public class CustomerLicenseKey
    {
        public required string Id { get; set; }
        public required string Key { get; set; }
        public required string ProductId { get; set; }
        public required int MaxActivations { get; set; }
        public required int Activations { get; set; }
        public required bool Activated { get; set; }
        public string? ExpirationDate { get; set; }
    }

    /// <summary>
    /// Response structure for a successful getCustomerWithKeys API call.
    /// </summary>
    public class GetCustomerWithKeysResponse
    {
        public required string Action { get; set; }
        public required bool Status { get; set; }
        public required CustomerWithKeysData Data { get; set; }
        public required int Code { get; set; }
    }

    public class CustomerWithKeysData
    {
        public required Customer Customer { get; set; }
        public required List<CustomerLicenseKey> LicenseKeys { get; set; }
    }

    /// <summary>
    /// Parameters for the updateCustomer API endpoint.
    /// </summary>
    public class UpdateCustomerParams
    {
        public required string CustomerId { get; set; }  // Required: The customer ID
        public string? Name { get; set; }       // Optional: Updated customer name
        public string? Email { get; set; }      // Optional: Updated customer email
        public bool? Active { get; set; }    // Optional: Customer active status
    }

    /// <summary>
    /// Response structure for a successful updateCustomer API call.
    /// </summary>
    public class UpdateCustomerResponse
    {
        public required string Action { get; set; }
        public required bool Status { get; set; }
        public required string Message { get; set; }
        public required Customer Data { get; set; }
        public required int Code { get; set; }
    }

    /// <summary>
    /// Parameters for the toggleCustomerStatus API endpoint.
    /// </summary>
    public class ToggleCustomerStatusParams
    {
        public required string CustomerId { get; set; }  // Required: The customer ID
    }

    /// <summary>
    /// Response structure for a successful toggleCustomerStatus API call.
    /// </summary>
    public class ToggleCustomerStatusResponse
    {
        public required string Action { get; set; }      // Action performed (e.g., "toggleActive")
        public required bool Status { get; set; }     // Success status
        public required string Message { get; set; }     // Status message (e.g., "Customer disabled")
        public required int Code { get; set; }        // API response code
    }

    /// <summary>
    /// Parameters for the getCustomerById API endpoint.
    /// </summary>
    public class GetCustomerByIdParams
    {
        public required string CustomerId { get; set; }  // Required: The customer ID
    }

    /// <summary>
    /// Response structure for a successful getCustomerById API call.
    /// </summary>
    public class GetCustomerByIdResponse
    {
        public required string Action { get; set; }      // Action performed (e.g., "getCustomerById")
        public required bool Status { get; set; }     // Success status
        public required List<Customer> Data { get; set; }    // Array containing the customer object
        public required int Code { get; set; }        // API response code
    }

    /// <summary>
    /// Parameters for the deleteCustomer API endpoint.
    /// </summary>
    public class DeleteCustomerParams
    {
        public required string CustomerId { get; set; }  // Required: The customer ID
    }

    /// <summary>
    /// Response structure for a successful deleteCustomer API call.
    /// </summary>
    public class DeleteCustomerResponse
    {
        public required string Action { get; set; }      // Action performed (e.g., "deleteCustomer")
        public required bool Status { get; set; }     // Success status
        public required string Message { get; set; }     // Status message (e.g., "Customer deleted")
        public required int Code { get; set; }        // API response code
    }
}