# KeyMint C# SDK

Welcome to the official KeyMint SDK for C#! This library provides a simple and convenient way to interact with the KeyMint API, allowing you to manage license keys for your applications with ease.

## ✨ Features

*   **Simple & Intuitive**: A clean and modern API that is easy to learn and use.
*   **Strongly Typed**: Full type safety with comprehensive C# classes and interfaces.
*   **Comprehensive**: Covers all the essential KeyMint API endpoints.
*   **Well-Documented**: Clear and concise documentation with plenty of examples.
*   **Error Handling**: Standardized error handling to make debugging a breeze.
*   **Async/Await Support**: Built with modern async patterns for optimal performance.

## 🚀 Quick Start

Here's a complete example of how to use the SDK to create and activate a license key:

```csharp
using Keymint.CsharpSdk;
using Keymint.CsharpSdk.Services;

class Program
{
    static async Task Main(string[] args)
    {
        var accessToken = Environment.GetEnvironmentVariable("KEYMINT_ACCESS_TOKEN");
        var productId = Environment.GetEnvironmentVariable("KEYMINT_PRODUCT_ID");
        
        if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(productId))
        {
            Console.Error.WriteLine("Please set the KEYMINT_ACCESS_TOKEN and KEYMINT_PRODUCT_ID environment variables.");
            return;
        }

        var sdk = new KeyMintSDK(accessToken);

        try
        {
            // 1. Create a new license key
            var createResponse = await sdk.CreateKey(new CreateKeyParams
            {
                ProductId = productId,
                MaxActivations = "5" // Optional: Maximum number of activations
            });
            var licenseKey = createResponse.Key;
            Console.WriteLine($"Key created: {licenseKey}");

            // 2. Activate the license key
            var activateResponse = await sdk.ActivateKey(new ActivateKeyParams
            {
                ProductId = productId,
                LicenseKey = licenseKey,
                HostId = "UNIQUE_DEVICE_ID"
            });
            Console.WriteLine($"Key activated: {activateResponse.Message}");
        }
        catch (KeyMintApiException ex)
        {
            Console.Error.WriteLine($"API Error: {ex.Message} (Code: {ex.ErrorCode})");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"An unexpected error occurred: {ex.Message}");
        }
    }
}
```

## 📦 Installation

### Package Reference

Add the package to your `.csproj` file:

```xml
<PackageReference Include="KeyMint.CSharp.Sdk" Version="1.0.0" />
```

### Package Manager Console

```bash
Install-Package KeyMint.CSharp.Sdk
```

### .NET CLI

```bash
dotnet add package KeyMint.CSharp.Sdk
```

## 🛠️ Usage

### Initialization

First, create an instance of `KeyMintSDK` with your access token. You can find your access token in your [KeyMint dashboard](https://app.keymint.dev/dashboard/developer/access-tokens).

```csharp
using Keymint.CsharpSdk;

var accessToken = Environment.GetEnvironmentVariable("KEYMINT_ACCESS_TOKEN");
if (string.IsNullOrEmpty(accessToken))
{
    throw new Exception("Please set the KEYMINT_ACCESS_TOKEN environment variable.");
}

var sdk = new KeyMintSDK(accessToken);
```

### API Methods

All methods are asynchronous and return a `Task<T>`.

#### License Key Management

| Method          | Description                                     |
| --------------- | ----------------------------------------------- |
| `CreateKey`     | Creates a new license key.                      |
| `ActivateKey`   | Activates a license key for a device.           |
| `DeactivateKey` | Deactivates a device from a license key.        |
| `GetKey`        | Retrieves detailed information about a key.     |
| `BlockKey`      | Blocks a license key.                           |
| `UnblockKey`    | Unblocks a previously blocked license key.      |

#### Customer Management

| Method                | Description                                       |
| --------------------- | ------------------------------------------------- |
| `CreateCustomer`      | Creates a new customer.                           |
| `GetAllCustomers`     | Retrieves all customers.                          |
| `GetCustomerById`     | Gets a specific customer by ID.                   |
| `GetCustomerWithKeys` | Gets a customer along with their license keys.   |
| `UpdateCustomer`      | Updates an existing customer's information.       |
| `ToggleCustomerStatus`| Toggles a customer's active status.              |
| `DeleteCustomer`      | Permanently deletes a customer and their keys.   |

For more detailed information about the API methods and their parameters, please refer to the [API Reference](#api-reference) section below.

## 🚨 Error Handling

If an API call fails, the SDK will throw a `KeyMintApiException` that contains detailed error information.

```csharp
try
{
    var response = await sdk.CreateKey(new CreateKeyParams
    {
        ProductId = productId
    });
}
catch (KeyMintApiException ex)
{
    Console.Error.WriteLine($"API Error: {ex.Message}");
    Console.Error.WriteLine($"Error Code: {ex.ErrorCode}");
    Console.Error.WriteLine($"HTTP Status: {ex.StatusCode}");
    
    if (ex.ApiError != null)
    {
        Console.Error.WriteLine($"API Error Message: {ex.ApiError.Message}");
        Console.Error.WriteLine($"API Error Code: {ex.ApiError.Code}");
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"An unexpected error occurred: {ex.Message}");
}
```

## 📋 Examples

### Customer Management

```csharp
// Create a new customer
var customer = await sdk.CreateCustomer(new CreateCustomerParams
{
    Name = "John Doe",
    Email = "john@example.com"
});

// Get all customers
var customers = await sdk.GetAllCustomers();

// Get a specific customer by ID
var customerById = await sdk.GetCustomerById(new GetCustomerByIdParams
{
    CustomerId = "customer_123"
});

// Get customer with their license keys
var customerWithKeys = await sdk.GetCustomerWithKeys(new GetCustomerWithKeysParams
{
    CustomerId = customer.Data.Id
});

// Update customer
var updatedCustomer = await sdk.UpdateCustomer(new UpdateCustomerParams
{
    CustomerId = customer.Data.Id,
    Name = "John Smith",
    Email = "john.smith@example.com"
});

// Toggle customer status (enable/disable)
var toggleResponse = await sdk.ToggleCustomerStatus(new ToggleCustomerStatusParams
{
    CustomerId = customer.Data.Id
});

// Delete customer permanently (irreversible!)
var deleteResponse = await sdk.DeleteCustomer(new DeleteCustomerParams
{
    CustomerId = customer.Data.Id
});
```

### Creating a License Key with a New Customer

```csharp
var licenseResponse = await sdk.CreateKey(new CreateKeyParams
{
    ProductId = productId,
    MaxActivations = "3", // Optional
    NewCustomer = new NewCustomer
    {
        Name = "Jane Doe",
        Email = "jane@example.com"
    }
});
```

## 🔒 Security Best Practices

**Never hardcode your access tokens!** Always use environment variables or secure configuration:

1. **Use environment variables**:
   ```bash
   set KEYMINT_ACCESS_TOKEN=your_actual_token_here
   set KEYMINT_PRODUCT_ID=your_product_id_here
   ```

2. **Use secure configuration** in your `appsettings.json`:
   ```json
   {
     "KeyMint": {
       "AccessToken": "your_actual_token_here",
       "ProductId": "your_product_id_here"
     }
   }
   ```

3. **Use dependency injection** (recommended for ASP.NET Core applications):
   ```csharp
   services.AddSingleton<KeyMintSDK>(provider =>
   {
       var config = provider.GetRequiredService<IConfiguration>();
       var accessToken = config["KeyMint:AccessToken"];
       return new KeyMintSDK(accessToken);
   });
   ```

⚠️ **Important**: Never commit sensitive information like access tokens to version control.

## 📚 API Reference

### `KeyMintSDK(string accessToken, string baseUrl = "https://api.keymint.io")`

| Parameter     | Type     | Description                                                                 |
| ------------- | -------- | --------------------------------------------------------------------------- |
| `accessToken` | `string` | **Required.** Your KeyMint API access token.                                |
| `baseUrl`     | `string` | *Optional.* The base URL for the KeyMint API. Defaults to `https://api.keymint.io`. |

### `CreateKey(CreateKeyParams parameters)`

| Parameter        | Type          | Description                                                                 |
| ---------------- | ------------- | --------------------------------------------------------------------------- |
| `ProductId`      | `string`      | **Required.** The ID of the product.                                        |
| `MaxActivations` | `string`      | *Optional.* The maximum number of activations for the key.                  |
| `ExpiryDate`     | `string`      | *Optional.* The expiration date of the key in ISO 8601 format.              |
| `CustomerId`     | `string`      | *Optional.* The ID of an existing customer to associate with the key.       |
| `NewCustomer`    | `NewCustomer` | *Optional.* An object containing the name and email of a new customer.      |

### `ActivateKey(ActivateKeyParams parameters)`

| Parameter    | Type     | Description                                                                 |
| ------------ | -------- | --------------------------------------------------------------------------- |
| `ProductId`  | `string` | **Required.** The ID of the product.                                        |
| `LicenseKey` | `string` | **Required.** The license key to activate.                                  |
| `HostId`     | `string` | *Optional.* A unique identifier for the device.                             |
| `DeviceTag`  | `string` | *Optional.* A user-friendly name for the device.                            |

### `DeactivateKey(DeactivateKeyParams parameters)`

| Parameter    | Type     | Description                                                                 |
| ------------ | -------- | --------------------------------------------------------------------------- |
| `ProductId`  | `string` | **Required.** The ID of the product.                                        |
| `LicenseKey` | `string` | **Required.** The license key to deactivate.                                |
| `HostId`     | `string` | *Optional.* The ID of the device to deactivate. If omitted, all devices are deactivated. |

### `GetKey(GetKeyParams parameters)`

| Parameter    | Type     | Description                                                                 |
| ------------ | -------- | --------------------------------------------------------------------------- |
| `ProductId`  | `string` | **Required.** The ID of the product.                                        |
| `LicenseKey` | `string` | **Required.** The license key to retrieve.                                  |

### `BlockKey(BlockKeyParams parameters)`

| Parameter    | Type     | Description                                                                 |
| ------------ | -------- | --------------------------------------------------------------------------- |
| `ProductId`  | `string` | **Required.** The ID of the product.                                        |
| `LicenseKey` | `string` | **Required.** The license key to block.                                     |

### `UnblockKey(UnblockKeyParams parameters)`

| Parameter    | Type     | Description                                                                 |
| ------------ | -------- | --------------------------------------------------------------------------- |
| `ProductId`  | `string` | **Required.** The ID of the product.                                        |
| `LicenseKey` | `string` | **Required.** The license key to unblock.                                   |

### Customer Management Methods

#### `CreateCustomer(CreateCustomerParams parameters)`

| Parameter | Type     | Description                                      |
| --------- | -------- | ------------------------------------------------ |
| `Name`    | `string` | **Required.** The customer's name.              |
| `Email`   | `string` | **Required.** The customer's email address.     |

#### `GetAllCustomers()`

No parameters required. Returns a list of all customers.

#### `GetCustomerById(GetCustomerByIdParams parameters)`

| Parameter    | Type     | Description                              |
| ------------ | -------- | ---------------------------------------- |
| `CustomerId` | `string` | **Required.** The ID of the customer.   |

#### `GetCustomerWithKeys(GetCustomerWithKeysParams parameters)`

| Parameter    | Type     | Description                              |
| ------------ | -------- | ---------------------------------------- |
| `CustomerId` | `string` | **Required.** The ID of the customer.   |

#### `UpdateCustomer(UpdateCustomerParams parameters)`

| Parameter    | Type      | Description                                           |
| ------------ | --------- | ----------------------------------------------------- |
| `CustomerId` | `string`  | **Required.** The ID of the customer to update.      |
| `Name`       | `string`  | *Optional.* Updated customer name.                    |
| `Email`      | `string`  | *Optional.* Updated customer email.                   |
| `Active`     | `bool?`   | *Optional.* Whether the customer is active.          |

#### `ToggleCustomerStatus(ToggleCustomerStatusParams parameters)`

| Parameter    | Type      | Description                                           |
| ------------ | --------- | ----------------------------------------------------- |
| `CustomerId` | `string`  | **Required.** The ID of the customer to toggle.      |

#### `DeleteCustomer(DeleteCustomerParams parameters)`

| Parameter    | Type      | Description                                           |
| ------------ | --------- | ----------------------------------------------------- |
| `CustomerId` | `string`  | **Required.** The ID of the customer to delete permanently. |

⚠️ **Warning**: `DeleteCustomer` permanently deletes the customer and all associated license keys. This action cannot be undone.

## 📜 License

This SDK is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## 🤝 Support

If you encounter any issues or have questions about using this SDK, please:

1. Check the [API documentation](https://docs.keymint.dev) for detailed information about the API endpoints
2. Search for existing issues in the GitHub repository
3. Create a new issue with detailed information about your problem

## 🚀 Changelog

### Version 1.0.0
- Initial release of the C# SDK
- Full support for all KeyMint API endpoints
- Comprehensive error handling with `KeyMintApiException`
- Async/await support for all API calls
- Strongly typed request and response models

This SDK provides complete coverage of the KeyMint API with modern C# patterns and comprehensive error handling for production applications.