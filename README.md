# Keymint .NET

A professional, production-ready SDK for integrating with the Keymint API in C# and other .NET languages. Provides robust, async-first access to all major Keymint features, with strong typing and modern error handling.

## Features
- **Async/await**: All API calls are asynchronous.
- **Strongly typed**: Full support for .NET types and JSON serialization.
- **Consistent error handling**: All API errors are returned as structured `KeyMintResult` objects.
- **Modern .NET**: Built for .NET 6.0, 7.0, 8.0, and 9.0.

## Installation
Add the SDK to your project:

```bash
dotnet add package KeyMint
```

## Usage

```csharp
using KeyMint.Services;

var accessToken = Environment.GetEnvironmentVariable("KEYMINT_ACCESS_TOKEN");
var productId = Environment.GetEnvironmentVariable("KEYMINT_PRODUCT_ID");

var client = new KeyMintSDK(accessToken);

// Example: Create a key with authorized hosts
var result = await client.CreateKey(new CreateKeyParams { 
    ProductId = productId,
    AllowedHosts = new List<string> { "machine-a" }
});

if (result.IsSuccess) {
    var key = result.Data.Key;
    // ...
}
```

## Error Handling
All SDK methods return a `KeyMintResult<T>`. Check `IsSuccess` before using the data. API errors are returned in the `Error` property.

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

### Customer Management

| Method                | Description                                      |
|-----------------------|--------------------------------------------------|
| `CreateCustomer`      | Creates a new customer.                          |
| `GetAllCustomers`     | Retrieves all customers.                         |
| `GetCustomerById`     | Gets a specific customer by ID.                  |
| `getCustomerWithKeys` | Gets a customer along with their license keys.   |
| `UpdateCustomer`      | Updates an existing customer's information.      |
| `ToggleCustomerStatus`| Toggles a customer's active status.              |
| `DeleteCustomer`      | Permanently deletes a customer and their keys.   |

## License
MIT

## Support
For help, see [Keymint API docs](https://docs.keymint.dev) or open an issue.