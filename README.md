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

Keymint API keys have a scope. Use two different keys, in two different places:

- A **`client`** key, bound to one product, goes in the app you ship. It can only activate and deactivate devices and run floating-license sessions.
- An **`admin`** key stays on your server. It creates, updates, signs, blocks and deletes keys and manages customers. **Never ship an admin key in an app.**

Anything compiled into an app can be extracted from it, so treat any key you ship as public.

### In your app (client key)

Get this machine's installation ID, then activate. Calling `ActivateKey` again with the same `HostId` reuses that device's slot, so you can call it at start-up to check the license is still valid.

```csharp
using KeyMint.Services;

// A client-scoped API key bound to this product. Never put an admin key here.
const string ClientApiKey = "YOUR_PRODUCT_BOUND_CLIENT_KEY";
const string ProductId = "YOUR_PRODUCT_ID";

var client = new KeyMintSDK(ClientApiKey);

// 1. Get this installation's ID (see Machine Identity below)
var hostId = KeyMintIdentity.GetOrCreateInstallationId();

var licenseKey = "the key your user entered";

// 2. Activate (or re-check) the license on this machine
var activation = await client.ActivateKey(new ActivateKeyParams {
    ProductId = ProductId,
    LicenseKey = licenseKey,
    HostId = hostId
});

if (activation.IsSuccess) {
    Console.WriteLine($"Activated: {activation.Data!.Message}");
}

// 3. Later, free this machine's slot (for example when the user moves to
//    another computer). Client keys must pass HostId.
var deactivation = await client.DeactivateKey(new DeactivateKeyParams {
    ProductId = ProductId,
    LicenseKey = licenseKey,
    HostId = hostId
});
```

`FloatingCheckout`, `FloatingHeartbeat` and `FloatingCheckin` also work with a client key.

### On your server (admin key)

Creating, updating, signing, blocking and deleting keys, and managing customers, need an admin key. Run this code on infrastructure you control and load the key from your server's environment or secret store.

```csharp
using KeyMint.Services;

// Admin key: server only. Never ship this key in an app.
var adminKey = Environment.GetEnvironmentVariable("KEYMINT_ADMIN_API_KEY")!;
var productId = Environment.GetEnvironmentVariable("KEYMINT_PRODUCT_ID")!;

var client = new KeyMintSDK(adminKey);

// Create a customer
var customer = await client.CreateCustomer(new CreateCustomerParams {
    Name = "Ada Lovelace",
    Email = "ada@example.com"
});

// Create a key. AllowedHosts is optional: to lock the key to one machine,
// pass the host ID your app reported to your server.
var result = await client.CreateKey(new CreateKeyParams {
    ProductId = productId,
    // AllowedHosts = new List<string> { hostIdFromYourApp }
});

if (result.IsSuccess) {
    var key = result.Data!.Key!;
    Console.WriteLine($"Created Key: {key}");

    // Block a key, and unblock it again
    await client.BlockKey(new BlockKeyParams { ProductId = productId, LicenseKey = key });
    await client.UnblockKey(new UnblockKeyParams { ProductId = productId, LicenseKey = key });
}
```

Customer management (`CreateCustomer`, `UpdateCustomer`, `ToggleCustomerStatus`, `DeleteCustomer`) also needs an admin key. The read calls (`GetKey`, `GetAllCustomers`, `GetCustomerById`, `GetCustomerWithKeys`) need at least a `read-only` key, which a `client` key is not, so keep those on your server too.

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

1. **Get the `.lic` file.** Download it from the dashboard (Offline License), or create it with `SignKey` **on your server**: it needs an admin key. While the machine is online, your app sends its host ID to your server, which signs the license and returns the file. `HostId` is the machine code the license is bound to.

   ```csharp
   // On your server, with an admin-key client:
   var signed = await adminClient.SignKey(new SignKeyParams {
       ProductId = productId,
       LicenseKey = licenseKey,
       HostId = hostIdFromYourApp
   });
   // signed.Data.File holds the .lic contents; send it to the app to save.
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
Keymint provides utilities to identify an installation for node-locking:

- `KeyMintIdentity.GetOrCreateInstallationId(storagePath)`: **Recommended** as the `HostId`. On first call it combines a random GUID, the `GetMachineId()` fingerprint (empty if none could be read) and the current time, and saves that value to `storagePath` (default `~/.keymint/installation-id`, under the user profile folder). It returns the SHA-256 hash of the saved value, so later calls return the same ID across restarts and app updates, and hardware changes don't affect it. It is random, not derived from the hardware: a new ID is created if the file is deleted, after an OS reinstall, or for another OS user (each has their own profile folder). If the file can't be written, no exception is thrown and each call produces a new ID.
- `KeyMintIdentity.GetMachineId()`: A best-effort SHA-256 fingerprint of the first usable value among the BIOS/hardware UUID, the OS machine ID (on macOS, the hardware serial number) and the first active network interface's MAC address. It can change after hardware changes or an OS reinstall, can collide on virtual machines, and returns `null` if nothing could be read.

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
| `GetCustomerWithKeys` | Lists the license keys belonging to a customer.  |
| `UpdateCustomer`      | Updates an existing customer's information.      |
| `ToggleCustomerStatus`| Toggles a customer's active status.              |
| `DeleteCustomer`      | Deletes a customer. Their license keys are kept; use `DeleteKey` to remove them. |

### Webhook Verification

| Method                  | Description                                      |
|-------------------------|--------------------------------------------------|
| `VerifyWebhookSignature`| Verifies the signature of a webhook request payload. |

## Error Handling

SDK methods don't throw for API errors. They return a `KeyMintResult<T>`, and when `IsSuccess` is false, `Error` is a `KeyMintApiError` with `Message`, `Code`, `ErrorCode` and `Status`. Branch on `ErrorCode`, a stable string such as `license_invalid_or_forbidden` or `rate_limited`, not on `Message`, which is written for people and may be reworded. See the [error code reference](https://keymint.dev/docs/api-reference/error-codes) for the full list.

```csharp
var result = await client.ActivateKey(new ActivateKeyParams {
    ProductId = productId,
    LicenseKey = licenseKey,
    HostId = hostId
});

if (result.IsSuccess) {
    Console.WriteLine($"Activated: {result.Data!.Message}");
} else {
    switch (result.Error!.ErrorCode) {
        case "license_invalid_or_forbidden":
            // Rejected: ask the user to re-enter the key or contact support.
            break;
        case "activation_limit_reached":
            // Ask the user to deactivate another device.
            break;
        case "rate_limited":
        case "service_unavailable":
            // No answer yet: keep the last known state and retry later.
            break;
        default:
            Console.WriteLine($"Keymint error {result.Error.ErrorCode ?? result.Error.Code.ToString()} (HTTP {result.Error.Status}): {result.Error.Message}");
            break;
    }
}
```

`ErrorCode` is `null` when the response has none, for example a non-JSON body from a proxy or a network failure, where `Code` is `-1`. `Code` and `Error` (the legacy `error` field) are unchanged.

## Idempotency

Mutating SDK methods accept an idempotency key so you can safely retry a request after a network drop. Pass a `RequestOptions` instance as the optional second argument:

```csharp
// On your server: CreateKey needs an admin key.
var result = await client.CreateKey(new CreateKeyParams {
    ProductId = productId,
}, new RequestOptions {
    IdempotencyKey = "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d"
});
```

Use a fresh random UUID for each operation and reuse it only to retry that same operation. Never derive it from the license key or host ID: a replay returns the original response, which can include a newly created license key.

The floating-license methods (`FloatingCheckout`, `FloatingHeartbeat`, `FloatingCheckin`) are the exception: the server ignores `Idempotency-Key` on them, so nothing is stored or replayed and a retry is a new request. Each heartbeat and check-in spends the single-use nonce it carries. If a checkout response is lost, the seat stays reserved until its lease expires.

## License
MIT

## Support
For help, see [Keymint API docs](https://keymint.dev/docs) or open an issue.
