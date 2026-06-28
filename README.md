# Keymint .NET

A professional, production-ready SDK for integrating with the Keymint API in C# and other .NET languages. Provides robust, async-first access to all major Keymint features, with strong typing and modern error handling.

## Features
- **Async/await**: All API calls are asynchronous.
- **Strongly typed**: Full support for .NET types and JSON serialization.
- **Consistent error handling**: All API errors are returned as structured `KeyMintResult` objects.
- **Machine Identity**: Built-in utilities for hardware fingerprinting and stable installation IDs.

## Installation
Add the SDK to your project:

```bash
dotnet add package KeyMint
```

## Usage

```csharp
using KeyMint.Services;

var apiKey = Environment.GetEnvironmentVariable("KEYMINT_API_KEY");
var productId = Environment.GetEnvironmentVariable("KEYMINT_PRODUCT_ID");

var client = new KeyMintSDK(apiKey);

// 1. Get a stable, unique ID for this machine
var hostId = KeyMintIdentity.GetOrCreateInstallationId();

// 2. Create a key authorized only for this machine
var result = await client.CreateKey(new CreateKeyParams { 
    ProductId = productId,
    AllowedHosts = new List<string> { hostId }
});

if (result.IsSuccess) {
    Console.WriteLine($"Created Key: {result.Data.Key}");
}
```

## Machine Identity
Keymint provides utilities to uniquely identify machines for node-locking:

- `KeyMintIdentity.GetOrCreateInstallationId()`: **Recommended.** Generates a stable UUID anchored to hardware and persists it to `~/.keymint/installation-id`.
- `KeyMintIdentity.GetMachineId()`: Generates a SHA-256 fingerprint based on BIOS UUID, OS machine ID, and MAC address.

## API Methods

### License Key Management

| Method          | Description                                     |
|-----------------|-------------------------------------------------|
| `CreateKey`     | Creates a new license key.                      |
| `ActivateKey`   | Activates a license key for a device.           |
| `DeactivateKey` | Deactivates a device from a license key.        |
| `GetKey`        | Retrieves detailed information about a key.     |
| `BlockKey`      | Blocks a license key.                           |
| `UnblockKey`    | Unblocks a previously blocked license key.      |
| `FloatingCheckout` | Checks out a floating license seat.           |
| `FloatingHeartbeat`| Sends a heartbeat to keep a session alive.    |
| `FloatingCheckin`  | Checks in a session, releasing the seat.      |

### Customer Management

| Method                | Description                                      |
|-----------------------|--------------------------------------------------|
| `CreateCustomer`      | Creates a new customer.                          |
| `GetAllCustomers`     | Retrieves all customers.                         |
| `GetCustomerById`     | Gets a specific customer by ID.                  |
| `GetCustomerWithKeys` | Gets a customer along with their license keys.   |
| `UpdateCustomer`      | Updates an existing customer's information.      |
| `ToggleCustomerStatus`| Toggles a customer's active status.              |
| `DeleteCustomer`      | Permanently deletes a customer and their keys.   |

### Webhook Verification

| Method                  | Description                                      |
|-------------------------|--------------------------------------------------|
| `VerifyWebhookSignature`| Verifies the signature of a webhook request payload. |

## Idempotency

All mutating SDK methods support idempotency keys to safely retry requests in case of network drops. Pass a `RequestOptions` instance as the optional second argument:

```csharp
var result = await client.CreateKey(new CreateKeyParams { 
    ProductId = productId,
}, new RequestOptions {
    IdempotencyKey = "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d"
});
```

## License
MIT

## Support
For help, see [Keymint API docs](https://docs.keymint.dev) or open an issue.