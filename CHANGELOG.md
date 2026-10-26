# Changelog

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
