# Keymint .NET

A professional, production-ready SDK for integrating with the Keymint API in C# and other .NET languages. Provides robust, async-first access to all major Keymint features, with strong typing and modern error handling.

Requires .NET 8 or later.

## Features
- **Async/await**: All API calls are asynchronous.
- **Strongly typed**: Full support for .NET types and JSON serialization.
- **Consistent error handling**: All API errors are returned as structured `KeyMintResult` objects.
- **Machine Identity**: Built-in utilities for hardware fingerprinting and stable installation IDs.
- **Offline licenses**: Verify signed `.lic` files locally with `OfflineLicenseVerifier`.

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

## License Expiry

`ActivateKey` and `FloatingCheckout` return `ExpirationDate`: when the license expires, as a UTC `DateTimeOffset?`, or `null` if it never expires. On `FloatingCheckout` this is separate from `ExpiresAt`, which is the floating seat lease.

```csharp
var activation = await client.ActivateKey(new ActivateKeyParams {
    ProductId = productId,
    LicenseKey = licenseKey,
    HostId = hostId
});

if (activation.IsSuccess) {
    var expiry = activation.Data.ExpirationDate;
    if (expiry is null) {
        Console.WriteLine("License never expires.");
    } else {
        var daysLeft = Math.Max(0, (int)Math.Ceiling((expiry.Value - DateTimeOffset.UtcNow).TotalDays));
        Console.WriteLine($"License expires {expiry.Value:yyyy-MM-dd} ({daysLeft} days left).");
    }
    // Persist the last value you received (e.g. in your app settings).
}
```

Store the last `ExpirationDate` you received. An expired license fails activation with the same generic error as an invalid key, so the stored date is what lets your app tell the user the license expired on that date instead of calling the key invalid.

## Offline Licenses

An offline license is a `.lic` file holding an Ed25519-signed token bound to one product and one machine. `OfflineLicenseVerifier.Verify` checks it locally, with no network call.

1. **Get the `.lic` file.** Download it from the dashboard (Offline License), or create it with `SignKey` (admin API key) while the machine is online. `HostId` is the machine code the license is bound to.

   ```csharp
   var signed = await client.SignKey(new SignKeyParams { // admin API key
       ProductId = productId,
       LicenseKey = licenseKey,
       HostId = hostId
   });
   // signed.Data.File holds the .lic contents; save it on the machine.
   ```

2. **Bundle your public key.** Copy the PEM from **Developer → Public Keys** in the dashboard and ship it with your app. It is a public key, so it is safe to ship; never ship an API key.

3. **Verify** with the product ID and this machine's code:

   ```csharp
   using KeyMint.Services;

   const string PublicKeyPem = """
       -----BEGIN PUBLIC KEY-----
       MCowBQYDK2VwAyEA...
       -----END PUBLIC KEY-----
       """;

   try
   {
       var license = OfflineLicenseVerifier.Verify(File.ReadAllText("license.lic"), new OfflineVerifyOptions
       {
           PublicKeys = new[] { PublicKeyPem },
           ProductId = productId,                                    // required
           MachineCode = KeyMintIdentity.GetOrCreateInstallationId(), // the HostId the file was signed for
           Now = TrustedNow(),                                       // optional, see clock rollback below
           // ClockSkew = TimeSpan.FromSeconds(60),                  // optional leeway on expiry, 0 to 1 hour
       });
       Console.WriteLine($"Licensed until {license.ExpiresAt:yyyy-MM-dd}");
   }
   catch (OfflineLicenseException ex)
   {
       switch (ex.Code)
       {
           case OfflineLicenseErrorCode.Expired:      /* ask the user to go online and renew */ break;
           case OfflineLicenseErrorCode.WrongMachine: /* the file was issued for another machine */ break;
           case OfflineLicenseErrorCode.WrongProduct: /* the file is for a different product */ break;
           default: /* Malformed, UnsupportedAlgorithm, UnknownKey, BadSignature, WrongType: not a genuine license */ break;
       }
   }
   ```

`Verify` accepts the `.lic` file contents or the bare token and returns an `OfflineLicense` with `LicenseKey`, `ProductId`, `MachineCode`, `KeyId`, `IssuedAt` (`DateTimeOffset?`) and `ExpiresAt`. `TryVerify(license, options, out var result, out var error)` does the same without throwing for an invalid license. On failure, `OfflineLicenseException.Code` is one of:

| Code | `CodeString` | Meaning |
|---|---|---|
| `Malformed` | `malformed` | Not a `.lic` file or token, or a required claim is missing |
| `UnsupportedAlgorithm` | `unsupported_algorithm` | Not signed with EdDSA |
| `UnknownKey` | `unknown_key` | Signed by a key not in `PublicKeys` |
| `BadSignature` | `bad_signature` | The signature does not match (the file was altered) |
| `WrongType` | `wrong_type` | Not an offline license token |
| `WrongProduct` | `wrong_product` | Issued for a different product |
| `WrongMachine` | `wrong_machine` | Issued for a different machine |
| `Expired` | `expired` | Past its expiry plus `ClockSkew` |

`CodeString` (or `Code.ToSpecString()`) is the same code the other Keymint SDKs use. The exception message never includes the license key.

`ProductId` is required because one signing key covers every product in your organization: without the check, a license for one product would unlock another. `ProductId` and `MachineCode` are compared exactly (no trimming or case folding). Pass more than one key in `PublicKeys` to accept licenses across a key rotation.

Invalid options are programming errors and throw `ArgumentException` (also from `TryVerify`): no public keys, a key that isn't an Ed25519 public key PEM, or an empty `ProductId` or `MachineCode`. A `ClockSkew` outside 0 to 1 hour throws `ArgumentOutOfRangeException`.

**Clock rollback.** The SDK can't tell if the user set the system clock back. Store the latest time your app has trusted (for example after each successful check or online call) and pass `Now = max(system time, last trusted time)`:

```csharp
static DateTimeOffset TrustedNow()
{
    var now = DateTimeOffset.UtcNow;
    var lastTrusted = LoadLastTrustedTime(); // your app's storage
    var trusted = now > lastTrusted ? now : lastTrusted;
    SaveLastTrustedTime(trusted);
    return trusted;
}
```

An offline license stays valid until it expires; blocking the key online does not revoke a file already issued.

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
| `DeleteKey`     | Permanently deletes a license key.              |
| `UpdateKey`     | Updates an existing license key.                |
| `SignKey`       | Signs a key for offline (air-gapped) validation using an admin API key.|
| `OfflineLicenseVerifier.Verify` | Verifies a signed `.lic` file locally, with no network call. See [Offline Licenses](#offline-licenses). |
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
