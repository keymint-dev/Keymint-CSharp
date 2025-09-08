# KeyMint C# SDK

A professional, production-ready SDK for integrating with the KeyMint API in C#. Provides robust, async-first access to all major KeyMint features, with strong typing and modern error handling.

## Features
- **Async/await**: All API calls are asynchronous.
- **Strongly typed**: Request and response models for all endpoints.
- **Consistent error handling**: All API errors are returned as structured result objects, never thrown as exceptions.
- **Logging**: Integrates with Microsoft.Extensions.Logging for professional diagnostics.

## Installation
Add the SDK to your project:

```
dotnet add package KeyMint
```

## Usage

```csharp
using KeyMint;
using KeyMint.Services;

var accessToken = Environment.GetEnvironmentVariable("KEYMINT_ACCESS_TOKEN");
var productId = Environment.GetEnvironmentVariable("KEYMINT_PRODUCT_ID");

if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(productId))
{
    // Handle missing configuration
    throw new InvalidOperationException("Please set the KEYMINT_ACCESS_TOKEN and KEYMINT_PRODUCT_ID environment variables.");
}

var sdk = new KeyMintSDK(accessToken);

// Example: Create a key
var result = await sdk.CreateKey(new CreateKeyParams { ProductId = productId });
if (result.IsSuccess)
{
    var key = result.Data?.Key;
    // ...
}
else
{
    // Log or handle error: result.Error
}
```

## Error Handling
All SDK methods return `KeyMintResult<T>`. Check `IsSuccess` before using `Data`. If `IsSuccess` is false, inspect `Error` for details. No API errors are thrown as exceptions.

## API Methods

All methods are asynchronous and return a `Task<KeyMintResult<T>>`. Check `IsSuccess` before using `Data`. If `IsSuccess` is false, inspect `Error` for details.

### License Key Management

| Method                | Description                                      |
|---------------------- |-------------------------------------------------|
| `CreateKey`           | Creates a new license key.                       |
| `ActivateKey`         | Activates a license key for a device.            |
| `DeactivateKey`       | Deactivates a device from a license key.         |
| `GetKey`              | Retrieves detailed information about a key.      |
| `BlockKey`            | Blocks a license key.                            |
| `UnblockKey`          | Unblocks a previously blocked license key.       |

### Customer Management

| Method                   | Description                                      |
|--------------------------|-------------------------------------------------|
| `CreateCustomer`         | Creates a new customer.                         |
| `GetAllCustomers`        | Retrieves all customers.                        |
| `GetCustomerById`        | Gets a specific customer by ID.                 |
| `GetCustomerWithKeys`    | Gets a customer along with their license keys.  |
| `UpdateCustomer`         | Updates an existing customer's information.     |
| `ToggleCustomerStatus`   | Toggles a customer's active status.             |
| `DeleteCustomer`         | Permanently deletes a customer and their keys.  |

For detailed parameter and response types, see the [KeyMint API docs](https://docs.keymint.dev) or use IntelliSense in your IDE.



## License
MIT

## Support
For help, see [KeyMint API docs](https://docs.keymint.dev) or open an issue.