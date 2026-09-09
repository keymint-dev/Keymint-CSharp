namespace KeyMint.Services
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
        public required string Name { get; set; }

        public string? Email { get; set; } // Optional: Email of the new customer
    }

    /// <summary>
    /// Key format options for custom license key shapes.
    /// </summary>
    public class KeyFormat
    {
        public int? Sections { get; set; }
        public int? SectionLength { get; set; }
        public string? Separator { get; set; }
        public string? Charset { get; set; }
        public string? Prefix { get; set; }
        public string? Suffix { get; set; }
        public string? Case { get; set; } // "upper", "lower", or "mixed"
    }

    /// <summary>
    /// Parameters for the createKey API endpoint.
    /// </summary>
    public class CreateKeyParams
    {
        public required string ProductId { get; set; } // Required: The unique identifier of the product.
        public string? MaxActivations { get; set; }    // Optional: The maximum number of times the key can be activated.
        public string? ExpiryDate { get; set; } // ISO 8601 string, not DateTime
        public string? CustomerId { get; set; }   // Optional: The ID of an existing customer to associate with the key.
        public string? VersionId { get; set; }    // Optional: The ID of a specific product version to associate with the key.
        public Dictionary<string, object>? Metadata { get; set; } // Optional: Custom metadata
        public NewCustomer? NewCustomer { get; set; }  // Optional: An object to create and associate a new customer with the key.
        public List<string>? AllowedHosts { get; set; } // Optional: List of authorized machine IDs.
        public KeyFormat? Format { get; set; }    // Optional: Custom key format
        public string? AmountKeys { get; set; }   // Optional: Number of keys to generate at once
        public string? LicenseType { get; set; }  // Optional: "node-locked" or "floating"
        public int? MaxConcurrentSessions { get; set; } // Optional: Max concurrent floating sessions
        public int? HeartbeatInterval { get; set; }     // Optional: Floating heartbeat interval in seconds (min 60)
        public int? SessionLeaseDuration { get; set; }  // Optional: Floating session lease duration in seconds (min 300)
        /// <summary>
        /// Returns true if the required fields are set.
        /// </summary>
        public bool IsValid() => !string.IsNullOrWhiteSpace(ProductId);
    }

    /// <summary>
    /// Response structure for a successful createKey API call.
    /// </summary>
    public class CreateKeyResponse
    {
        public required int Code { get; set; } // API response code (e.g., 0 for success)
        public string? Key { get; set; }  // The generated license key for single-key creation
        public List<string>? Keys { get; set; } // Generated license keys for bulk creation
    }

    /// <summary>
    /// Standard error response structure from the KeyMint API.
    /// </summary>
    public class KeyMintApiError
    {
        public string Message { get; set; } = "Keymint API request failed"; // Descriptive error message
        public int Code { get; set; }    // API numeric error code
        public int? Status { get; set; }  // HTTP status code, optional
        public KeyMintApiErrorDetails? Error { get; set; }
    }

    /// <summary>
    /// Detailed error information returned by the current Keymint API envelope.
    /// </summary>
    public class KeyMintApiErrorDetails
    {
        public string? Code { get; set; }
        public string? Message { get; set; }
        public object? Details { get; set; }
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
        public ActivationLicensee? Licensee { get; set; } // Optional: Customer name+email to set during activation
        public string? Version { get; set; }    // Optional: Product version string (max 32 chars)

        public bool IsValid() => !string.IsNullOrWhiteSpace(ProductId) && !string.IsNullOrWhiteSpace(LicenseKey);
    }

    /// <summary>
    /// Licensee info set during key activation.
    /// </summary>
    public class ActivationLicensee
    {
        public required string Name { get; set; }
        public required string Email { get; set; }
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
        public List<string>? AllowedHosts { get; set; } // Optional: List of authorized machine IDs.
        public Dictionary<string, object>? Metadata { get; set; }
        public string? VersionId { get; set; }
        public ActivationVersion? Version { get; set; }
    }

    /// <summary>
    /// Product version information returned during activation.
    /// </summary>
    public class ActivationVersion
    {
        public required string Version { get; set; }
    }

    /// <summary>
    /// Parameters for the deactivateKey API endpoint.
    /// </summary>
    public class DeactivateKeyParams
    {
        public required string ProductId { get; set; }  // Required: The unique identifier of the product.
        public required string LicenseKey { get; set; } // Required: The license key to deactivate.
        public string? HostId { get; set; }     // Optional: The unique identifier of the device to deactivate. If omitted, all devices are deactivated.

        public bool IsValid() => !string.IsNullOrWhiteSpace(ProductId) && !string.IsNullOrWhiteSpace(LicenseKey);
    }

    /// <summary>
    /// Response structure for a successful deactivateKey API call.
    /// </summary>
    public class DeactivateKeyResponse
    {
        public required string Message { get; set; } // Confirmation message (e.g., "Device deactivated")
        public required int Code { get; set; }    // API response code (e.g., 0 for success)
        public int? DevicesRemoved { get; set; }
    }

    /// <summary>
    /// Device details included in the GetKeyResponse.
    /// </summary>
    public class DeviceDetails
    {
        public required string HostId { get; set; }           // Updated field name
        public string? DeviceTag { get; set; }       // Updated field name  
        public string? IpAddress { get; set; }       // Updated field name
        public DateTime ActivationTime { get; set; }   // Changed from string to DateTime
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
        public DateTime? ExpirationDate { get; set; }  // Changed from string? to DateTime?
        public List<string>? AllowedHosts { get; set; } // Optional: List of authorized machine IDs.
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

        public bool IsValid() => !string.IsNullOrWhiteSpace(ProductId) && !string.IsNullOrWhiteSpace(LicenseKey);
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

        public bool IsValid() => !string.IsNullOrWhiteSpace(ProductId) && !string.IsNullOrWhiteSpace(LicenseKey);
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

        public bool IsValid() => !string.IsNullOrWhiteSpace(ProductId) && !string.IsNullOrWhiteSpace(LicenseKey);
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

        public bool IsValid() => !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Email);
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
        public DateTime CreatedAt { get; set; } // Changed from string to DateTime
        public DateTime UpdatedAt { get; set; } // Changed from string to DateTime
        public string? CreatedBy { get; set; }
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

        public bool IsValid() => !string.IsNullOrWhiteSpace(CustomerId);
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
        public DateTime? ExpirationDate { get; set; } // Changed from string? to DateTime?
        public List<string>? AllowedHosts { get; set; } // Optional: List of authorized machine IDs.
    }

    /// <summary>
    /// Response structure for a successful getCustomerWithKeys API call (flat list of license keys).
    /// </summary>
    public class GetCustomerWithKeysResponse
    {
        public List<CustomerLicenseKey>? Data { get; set; } // The actual response is a flat LicenseKey[]
    }

    /// <summary>
    /// Parameters for the updateCustomer API endpoint.
    /// </summary>
    public class UpdateCustomerParams
    {
        public required string CustomerId { get; set; }  // Required: The customer ID
        public string? Name { get; set; }       // Optional: Updated customer name
        public string? Email { get; set; }      // Optional: Updated customer email

        public bool IsValid() => !string.IsNullOrWhiteSpace(CustomerId);
    }

    /// <summary>
    /// Response structure for a successful updateCustomer API call.
    /// </summary>
    public class UpdateCustomerResponse
    {
        public required string Action { get; set; }
        public required bool Status { get; set; }
        public string? Message { get; set; }
        public Customer? Data { get; set; }
        public required int Code { get; set; }
    }

    /// <summary>
    /// Parameters for the toggleCustomerStatus API endpoint.
    /// </summary>
    public class ToggleCustomerStatusParams
    {
        public required string CustomerId { get; set; }  // Required: The customer ID

        public bool IsValid() => !string.IsNullOrWhiteSpace(CustomerId);
    }

    /// <summary>
    /// Response structure for a successful toggleCustomerStatus API call.
    /// </summary>
    public class ToggleCustomerStatusResponse
    {
        public string? Action { get; set; }
        public required bool Status { get; set; }     // Success status
        public string? Message { get; set; }
        public int? Code { get; set; }
        public string? CustomerName { get; set; }
        public bool? Active { get; set; }
    }

    /// <summary>
    /// Parameters for the getCustomerById API endpoint.
    /// </summary>
    public class GetCustomerByIdParams
    {
        public required string CustomerId { get; set; }  // Required: The customer ID

        public bool IsValid() => !string.IsNullOrWhiteSpace(CustomerId);
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

        public bool IsValid() => !string.IsNullOrWhiteSpace(CustomerId);
    }

    /// <summary>
    /// Response structure for a successful deleteCustomer API call.
    /// </summary>
    public class DeleteCustomerResponse
    {
        public required string Action { get; set; }      // Action performed (e.g., "deleteCustomer")
        public required bool Status { get; set; }     // Success status
        public string? Message { get; set; } // Optional, API may omit
        public required int Code { get; set; }        // API response code
    }

    /// <summary>
    /// Result wrapper for SDK calls, matching Python/Node.js SDKs (success or error, never throws for API errors)
    /// </summary>
    public class KeyMintResult<T>
    {
        public T? Data { get; }
        public KeyMintApiError? Error { get; }
        public bool IsSuccess => Error == null;

        private KeyMintResult(T data) { Data = data; }
        private KeyMintResult(KeyMintApiError error) { Error = error; }

        public static KeyMintResult<T> Success(T data) => new KeyMintResult<T>(data);
        public static KeyMintResult<T> Failure(KeyMintApiError error) => new KeyMintResult<T>(error);
    }

    /// <summary>
    /// Parameters for the floating license checkout API endpoint.
    /// </summary>
    public class FloatingCheckoutParams
    {
        public required string ProductId { get; set; }
        public required string LicenseKey { get; set; }
        public required string HostId { get; set; }
        public string? DeviceTag { get; set; }
        public string? UserIdentifier { get; set; }
        public object? Timestamp { get; set; }
        public string? Signature { get; set; }

        public bool IsValid() => !string.IsNullOrWhiteSpace(ProductId) && !string.IsNullOrWhiteSpace(LicenseKey) && !string.IsNullOrWhiteSpace(HostId);
    }

    /// <summary>
    /// Response structure for a successful floating license checkout API call.
    /// </summary>
    public class FloatingCheckoutResponse
    {
        public required int Code { get; set; }
        public required string Message { get; set; }
        public required string SessionId { get; set; }
        public required string SessionSecret { get; set; }
        public required string NextNonce { get; set; }
        public required string ExpiresAt { get; set; }
        public required int HeartbeatInterval { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
        public int? CurrentSessions { get; set; }
        public int? MaxSessions { get; set; }
        public string? LicenseeName { get; set; }
        public string? LicenseeEmail { get; set; }
    }

    /// <summary>
    /// Parameters for the floating license heartbeat API endpoint.
    /// </summary>
    public class FloatingHeartbeatParams
    {
        public required string ProductId { get; set; }
        public required string LicenseKey { get; set; }
        public required string SessionId { get; set; }
        public required object Timestamp { get; set; } // holds the rotating nonce string
        public required string Signature { get; set; }

        public bool IsValid() => !string.IsNullOrWhiteSpace(ProductId) && !string.IsNullOrWhiteSpace(LicenseKey) && !string.IsNullOrWhiteSpace(SessionId) && Timestamp != null && !string.IsNullOrWhiteSpace(Signature);
    }

    /// <summary>
    /// Response structure for a successful floating license heartbeat API call.
    /// </summary>
    public class FloatingHeartbeatResponse
    {
        public required int Code { get; set; }
        public required string Message { get; set; }
        public required string ExpiresAt { get; set; }
        public required string NextNonce { get; set; }
    }

    /// <summary>
    /// Parameters for the floating license checkin API endpoint.
    /// </summary>
    public class FloatingCheckinParams
    {
        public required string ProductId { get; set; }
        public required string LicenseKey { get; set; }
        public required string SessionId { get; set; }
        public required object Timestamp { get; set; } // holds the rotating nonce string
        public required string Signature { get; set; }

        public bool IsValid() => !string.IsNullOrWhiteSpace(ProductId) && !string.IsNullOrWhiteSpace(LicenseKey) && !string.IsNullOrWhiteSpace(SessionId) && Timestamp != null && !string.IsNullOrWhiteSpace(Signature);
    }

    /// <summary>
    /// Response structure for a successful floating license checkin API call.
    /// </summary>
    public class FloatingCheckinResponse
    {
        public required int Code { get; set; }
        public required string Message { get; set; }
    }

    /// <summary>
    /// Optional configuration parameters for Keymint API requests (e.g. idempotency keys).
    /// </summary>
    public class RequestOptions
    {
        public string? IdempotencyKey { get; set; }
    }

    /// <summary>
    /// Parameters for the updateKey API endpoint (PATCH /api/key).
    /// </summary>
    public class UpdateKeyParams
    {
        public required string ProductId { get; set; }
        public required string LicenseKey { get; set; }
        public object? MaxActivations { get; set; } // string or number
        public string? ExpiryDate { get; set; }
        public string? CustomerId { get; set; }
        public NewCustomer? NewCustomer { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
        public string? VersionId { get; set; }
        public List<string>? AllowedHosts { get; set; }
        public string? LicenseType { get; set; }
        public int? MaxConcurrentSessions { get; set; }
        public int? HeartbeatInterval { get; set; }
        public int? SessionLeaseDuration { get; set; }

        public bool IsValid() => !string.IsNullOrWhiteSpace(ProductId) && !string.IsNullOrWhiteSpace(LicenseKey);
    }

    /// <summary>
    /// Response structure for a successful updateKey API call.
    /// </summary>
    public class UpdateKeyResponse
    {
        public required int Code { get; set; }
        public required string Message { get; set; }
        public int? AffectedCount { get; set; }
    }

    /// <summary>
    /// Parameters for the signKey API endpoint (POST /api/key/sign).
    /// </summary>
    public class SignKeyParams
    {
        public required string ProductId { get; set; }
        public required string LicenseKey { get; set; }
        public required string HostId { get; set; }
        public int? Ttl { get; set; }

        public bool IsValid() => !string.IsNullOrWhiteSpace(ProductId) && !string.IsNullOrWhiteSpace(LicenseKey) && !string.IsNullOrWhiteSpace(HostId);
    }

    /// <summary>
    /// Response structure for a successful signKey API call.
    /// </summary>
    public class SignKeyResponse
    {
        public required string File { get; set; }
    }
}
