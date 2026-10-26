# Changelog

## Unreleased

### Added

- `DeleteKey`: permanently deletes a license key (`DELETE /key`). Use `BlockKey` to stop a key reversibly.
- `OfflineLicenseVerifier.Verify` and `TryVerify`: verify an offline license (`.lic` file from `SignKey` or the dashboard, or the bare token) locally against your public key PEMs, with no network call. `ProductId` and `MachineCode` are required and compared exactly, so a license for one product can't unlock another (one signing key covers every product in an organization). Returns `OfflineLicense`; failures throw `OfflineLicenseException` with an `OfflineLicenseErrorCode` (`CodeString` gives the shared snake_case code). Follows the shared Keymint offline-verifier spec and passes its 34 test vectors.

### Dependencies

- New dependency: `BouncyCastle.Cryptography` 2.7.0 (MIT, managed, about 6 MB) for Ed25519, which .NET 8 and 9 don't provide. An app that still references the old `Portable.BouncyCastle` package and uses its types directly may get ambiguous-type compile errors, since both packages define the `Org.BouncyCastle` namespaces; move to `BouncyCastle.Cryptography`.

## 1.6.0 - 2026-10-26

### Added

- `ActivateKeyResponse.ExpirationDate` and `FloatingCheckoutResponse.ExpirationDate` (`DateTimeOffset?`): the license expiry returned by `POST /key/activate` and `POST /key/checkout`, or `null` when the license never expires. On checkout this is separate from `ExpiresAt`, the floating seat lease.

## 1.5.0 - 2026-09-09

### Fixed

- Match offline signing, activation, bulk-key, deactivation, and customer response contracts to the current Keymint API.
- Send license keys through `x-license-key` for detailed lookup instead of placing them in request URLs.
- Parse current nested API error responses so callers receive the server's actionable message.
- Support raw-array customer-key responses and sanitized customer records.
- Support floating-session renewal proof fields.

### Added

- Support .NET 8 alongside .NET 9.
- Add an `HttpClient` constructor overload for dependency injection and testability.
- Add deterministic contract tests and live API validation covering licensing, floating sessions, offline signing, and customer CRUD.
