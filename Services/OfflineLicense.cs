using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

namespace KeyMint.Services;

/// <summary>
/// Why an offline license failed verification. Use <see cref="OfflineLicenseErrorCodeExtensions.ToSpecString"/>
/// for the snake_case code shared with the other Keymint SDKs.
/// </summary>
public enum OfflineLicenseErrorCode
{
    /// <summary>Not a .lic file or a three-segment token, or a required claim (<c>exp</c>, <c>sub</c>) is missing or the wrong type. (<c>malformed</c>)</summary>
    Malformed,
    /// <summary>The token's <c>alg</c> is not exactly <c>EdDSA</c>, or it has a <c>crit</c> header. (<c>unsupported_algorithm</c>)</summary>
    UnsupportedAlgorithm,
    /// <summary>The token's <c>kid</c> does not match any trusted public key. (<c>unknown_key</c>)</summary>
    UnknownKey,
    /// <summary>The Ed25519 signature is not valid for the trusted key. (<c>bad_signature</c>)</summary>
    BadSignature,
    /// <summary>The token is not an offline license (<c>type</c> is not <c>offline</c>). (<c>wrong_type</c>)</summary>
    WrongType,
    /// <summary>The license was issued for a different product. (<c>wrong_product</c>)</summary>
    WrongProduct,
    /// <summary>The license was issued for a different machine. (<c>wrong_machine</c>)</summary>
    WrongMachine,
    /// <summary>The license has expired (allowing for the clock skew). (<c>expired</c>)</summary>
    Expired,
}

/// <summary>Helpers for <see cref="OfflineLicenseErrorCode"/>.</summary>
public static class OfflineLicenseErrorCodeExtensions
{
    /// <summary>The snake_case reason code used by every Keymint SDK, e.g. <c>wrong_product</c>.</summary>
    public static string ToSpecString(this OfflineLicenseErrorCode code) => code switch
    {
        OfflineLicenseErrorCode.Malformed => "malformed",
        OfflineLicenseErrorCode.UnsupportedAlgorithm => "unsupported_algorithm",
        OfflineLicenseErrorCode.UnknownKey => "unknown_key",
        OfflineLicenseErrorCode.BadSignature => "bad_signature",
        OfflineLicenseErrorCode.WrongType => "wrong_type",
        OfflineLicenseErrorCode.WrongProduct => "wrong_product",
        OfflineLicenseErrorCode.WrongMachine => "wrong_machine",
        OfflineLicenseErrorCode.Expired => "expired",
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, "Unknown offline license error code."),
    };
}

/// <summary>
/// Thrown by <see cref="OfflineLicenseVerifier.Verify"/> when a license fails verification.
/// The message never contains the license key.
/// </summary>
public sealed class OfflineLicenseException : Exception
{
    /// <summary>Why verification failed.</summary>
    public OfflineLicenseErrorCode Code { get; }

    /// <summary>The snake_case reason code shared with the other Keymint SDKs, e.g. <c>expired</c>.</summary>
    public string CodeString => Code.ToSpecString();

    public OfflineLicenseException(OfflineLicenseErrorCode code)
        : base(DefaultMessage(code))
    {
        Code = code;
    }

    private static string DefaultMessage(OfflineLicenseErrorCode code) => code switch
    {
        OfflineLicenseErrorCode.Malformed => "Offline license is malformed.",
        OfflineLicenseErrorCode.UnsupportedAlgorithm => "Offline license uses an unsupported signing algorithm.",
        OfflineLicenseErrorCode.UnknownKey => "Offline license was not signed by a trusted public key.",
        OfflineLicenseErrorCode.BadSignature => "Offline license signature is invalid.",
        OfflineLicenseErrorCode.WrongType => "Token is not an offline license.",
        OfflineLicenseErrorCode.WrongProduct => "Offline license is for a different product.",
        OfflineLicenseErrorCode.WrongMachine => "Offline license is for a different machine.",
        OfflineLicenseErrorCode.Expired => "Offline license has expired.",
        _ => "Offline license verification failed.",
    };
}

/// <summary>Options for <see cref="OfflineLicenseVerifier.Verify"/>.</summary>
public sealed class OfflineVerifyOptions
{
    /// <summary>Default leeway allowed after <c>exp</c>.</summary>
    public static readonly TimeSpan DefaultClockSkew = TimeSpan.FromSeconds(60);

    /// <summary>Largest allowed <see cref="ClockSkew"/>.</summary>
    public static readonly TimeSpan MaxClockSkew = TimeSpan.FromHours(1);

    /// <summary>
    /// One or more trusted public keys as SPKI PEM strings (Dashboard → Developer → Public Keys).
    /// Pass several to accept licenses across a key rotation.
    /// </summary>
    public required IReadOnlyList<string> PublicKeys { get; set; }

    /// <summary>
    /// The product this app is licensed for. Must equal the license's product exactly. Required: one signing key
    /// covers every product in an organization, so without this check a license for one product would unlock another.
    /// </summary>
    public required string ProductId { get; set; }

    /// <summary>This machine's code (the <c>hostId</c> the license was signed for). Must match exactly.</summary>
    public required string MachineCode { get; set; }

    /// <summary>
    /// The time to check expiry against. Defaults to the system clock. To guard against clock rollback, pass
    /// the later of the system time and the last time your app trusted.
    /// </summary>
    public DateTimeOffset? Now { get; set; }

    /// <summary>Leeway allowed after the license's expiry. Default 60 seconds; 0 to 1 hour.</summary>
    public TimeSpan ClockSkew { get; set; } = DefaultClockSkew;
}

/// <summary>A verified offline license.</summary>
public sealed class OfflineLicense
{
    /// <summary>The license key the file was issued for (the token's <c>sub</c>).</summary>
    public required string LicenseKey { get; init; }

    /// <summary>The product the license is for.</summary>
    public required string ProductId { get; init; }

    /// <summary>The machine code the license is bound to.</summary>
    public required string MachineCode { get; init; }

    /// <summary>The ID of the public key that signed the license.</summary>
    public required string KeyId { get; init; }

    /// <summary>When the license was signed, if the token says.</summary>
    public DateTimeOffset? IssuedAt { get; init; }

    /// <summary>When the license expires (before clock skew).</summary>
    public required DateTimeOffset ExpiresAt { get; init; }
}

/// <summary>
/// Verifies Keymint offline licenses (<c>.lic</c> files from <c>SignKey</c> or the dashboard) locally, with no
/// network call. Implements the shared Keymint offline-license verifier spec.
/// </summary>
public static class OfflineLicenseVerifier
{
    private const string PemLabel = "PUBLIC KEY";

    /// <summary>
    /// Verifies an offline license and returns its claims.
    /// </summary>
    /// <param name="license">The <c>.lic</c> file contents, or the bare signed token.</param>
    /// <param name="options">Trusted public keys, expected product and machine, and optional time settings.</param>
    /// <exception cref="OfflineLicenseException">The license is not valid for these options; see <see cref="OfflineLicenseException.Code"/>.</exception>
    /// <exception cref="ArgumentException">The options are invalid: no public keys, a key that is not an Ed25519 SPKI PEM, or an empty product ID or machine code.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><see cref="OfflineVerifyOptions.ClockSkew"/> is outside 0 to 1 hour.</exception>
    public static OfflineLicense Verify(string license, OfflineVerifyOptions options)
    {
        var outcome = VerifyCore(license, options, out var result);
        if (outcome is { } code)
        {
            throw new OfflineLicenseException(code);
        }
        return result!;
    }

    /// <summary>
    /// Verifies an offline license without throwing for verification failures.
    /// Invalid options still throw, as in <see cref="Verify"/>.
    /// </summary>
    /// <returns><c>true</c> and the license in <paramref name="result"/>, or <c>false</c> and the reason in <paramref name="error"/>.</returns>
    public static bool TryVerify(string license, OfflineVerifyOptions options, out OfflineLicense? result, out OfflineLicenseErrorCode? error)
    {
        error = VerifyCore(license, options, out result);
        return error is null;
    }

    /// <summary>
    /// The key ID Keymint uses for a public key: base64url (no padding) of SHA-256 over its SPKI DER.
    /// </summary>
    /// <exception cref="ArgumentException">The PEM is not an Ed25519 SPKI public key.</exception>
    public static string ComputeKeyId(string publicKeyPem) => ParsePublicKey(publicKeyPem, nameof(publicKeyPem)).KeyId;

    private static OfflineLicenseErrorCode? VerifyCore(string license, OfflineVerifyOptions options, out OfflineLicense? result)
    {
        result = null;
        ArgumentNullException.ThrowIfNull(license);
        ArgumentNullException.ThrowIfNull(options);
        if (options.PublicKeys is null || options.PublicKeys.Count == 0)
        {
            throw new ArgumentException("PublicKeys must contain at least one key.", nameof(options));
        }
        if (string.IsNullOrEmpty(options.ProductId))
        {
            throw new ArgumentException("ProductId is required.", nameof(options));
        }
        if (string.IsNullOrEmpty(options.MachineCode))
        {
            throw new ArgumentException("MachineCode is required.", nameof(options));
        }
        var clockSkew = options.ClockSkew;
        if (clockSkew < TimeSpan.Zero || clockSkew > OfflineVerifyOptions.MaxClockSkew)
        {
            throw new ArgumentOutOfRangeException(nameof(options), clockSkew, "ClockSkew must be between 0 and 1 hour.");
        }
        var keys = new Dictionary<string, Ed25519PublicKeyParameters>(StringComparer.Ordinal);
        foreach (var pem in options.PublicKeys)
        {
            var parsed = ParsePublicKey(pem, nameof(options));
            keys[parsed.KeyId] = parsed.Key;
        }

        // 1. Parse: a .lic file (JSON) or a bare token.
        var token = TrimJs(license);
        if (token.StartsWith('{'))
        {
            string? signedKey = null;
            try
            {
                using var file = JsonDocument.Parse(token);
                if (file.RootElement.ValueKind == JsonValueKind.Object
                    && file.RootElement.TryGetProperty("signedKey", out var sk)
                    && sk.ValueKind == JsonValueKind.String)
                {
                    signedKey = sk.GetString();
                }
            }
            catch (JsonException)
            {
                return OfflineLicenseErrorCode.Malformed;
            }
            if (signedKey is null)
            {
                return OfflineLicenseErrorCode.Malformed;
            }
            token = signedKey;
        }
        var parts = token.Split('.');
        if (parts.Length != 3 || !IsBase64UrlSegment(parts[0]) || !IsBase64UrlSegment(parts[1]) || !IsBase64UrlSegment(parts[2]))
        {
            return OfflineLicenseErrorCode.Malformed;
        }
        var headerB64 = parts[0];
        var payloadB64 = parts[1];
        var signatureB64 = parts[2];
        using var header = DecodeJsonObject(headerB64);
        using var payload = DecodeJsonObject(payloadB64);
        if (header is null || payload is null)
        {
            return OfflineLicenseErrorCode.Malformed;
        }
        var h = header.RootElement;
        var p = payload.RootElement;

        // 2. Algorithm: exactly EdDSA, no critical extensions.
        if (!IsString(h, "alg", out var alg) || alg != "EdDSA" || h.TryGetProperty("crit", out _))
        {
            return OfflineLicenseErrorCode.UnsupportedAlgorithm;
        }

        // 3. Key: the kid must name a trusted key.
        if (!IsString(h, "kid", out var kid) || !keys.TryGetValue(kid, out var key))
        {
            return OfflineLicenseErrorCode.UnknownKey;
        }

        // 4. Signature over the segments exactly as received.
        var signature = TryDecodeBase64Url(signatureB64);
        if (signature is null || signature.Length != 64)
        {
            return OfflineLicenseErrorCode.BadSignature;
        }
        var signer = new Ed25519Signer();
        signer.Init(false, key);
        var signedBytes = Encoding.ASCII.GetBytes($"{headerB64}.{payloadB64}");
        signer.BlockUpdate(signedBytes, 0, signedBytes.Length);
        if (!signer.VerifySignature(signature))
        {
            return OfflineLicenseErrorCode.BadSignature;
        }

        // 5-7. Claims, compared exactly.
        if (!IsString(p, "type", out var type) || type != "offline")
        {
            return OfflineLicenseErrorCode.WrongType;
        }
        if (!IsString(p, "productId", out var productId) || productId != options.ProductId)
        {
            return OfflineLicenseErrorCode.WrongProduct;
        }
        if (!IsString(p, "machineCode", out var machineCode) || machineCode != options.MachineCode)
        {
            return OfflineLicenseErrorCode.WrongMachine;
        }

        // 8. Shape of the remaining claims, then expiry.
        if (!IsFiniteNumber(p, "exp", out var exp))
        {
            return OfflineLicenseErrorCode.Malformed;
        }
        if (!IsString(p, "sub", out var sub) || sub.Length == 0)
        {
            return OfflineLicenseErrorCode.Malformed;
        }

        // 9. now < exp + clockSkew.
        var now = options.Now ?? DateTimeOffset.UtcNow;
        var nowSeconds = now.ToUnixTimeMilliseconds() / 1000.0;
        if (!(nowSeconds < exp + clockSkew.TotalSeconds))
        {
            return OfflineLicenseErrorCode.Expired;
        }

        result = new OfflineLicense
        {
            LicenseKey = sub,
            ProductId = options.ProductId,
            MachineCode = options.MachineCode,
            KeyId = kid,
            IssuedAt = IsFiniteNumber(p, "iat", out var iat) ? FromUnixSeconds(iat) : null,
            ExpiresAt = FromUnixSeconds(exp),
        };
        return null;
    }

    private readonly record struct TrustedKey(string KeyId, Ed25519PublicKeyParameters Key);

    private static TrustedKey ParsePublicKey(string? pem, string paramName)
    {
        if (string.IsNullOrWhiteSpace(pem))
        {
            throw new ArgumentException("Public key must be an Ed25519 SPKI PEM string.", paramName);
        }
        if (!PemEncoding.TryFind(pem, out var fields) || pem[fields.Label] != PemLabel)
        {
            throw new ArgumentException("Public key must be a PEM with a \"BEGIN PUBLIC KEY\" block.", paramName);
        }
        AsymmetricKeyParameter parsed;
        try
        {
            var der = Convert.FromBase64String(pem[fields.Base64Data]);
            parsed = PublicKeyFactory.CreateKey(der);
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            throw new ArgumentException("Public key is not a valid SPKI public key.", paramName, e);
        }
        if (parsed is not Ed25519PublicKeyParameters ed25519)
        {
            throw new ArgumentException("Public key must be an Ed25519 key.", paramName);
        }
        // Hash the canonical re-encoding, as the server does.
        var spki = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(ed25519).GetDerEncoded();
        var kid = Base64UrlEncode(SHA256.HashData(spki));
        return new TrustedKey(kid, ed25519);
    }

    private static bool IsString(JsonElement obj, string name, out string value)
    {
        if (obj.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String)
        {
            value = el.GetString()!;
            return true;
        }
        value = string.Empty;
        return false;
    }

    private static bool IsFiniteNumber(JsonElement obj, string name, out double value)
    {
        value = 0;
        return obj.TryGetProperty(name, out var el)
            && el.ValueKind == JsonValueKind.Number
            && el.TryGetDouble(out value)
            && double.IsFinite(value);
    }

    private static DateTimeOffset FromUnixSeconds(double seconds)
    {
        var ms = Math.Floor(seconds * 1000.0);
        if (ms >= DateTimeOffset.MaxValue.ToUnixTimeMilliseconds())
        {
            return DateTimeOffset.MaxValue;
        }
        if (ms <= DateTimeOffset.MinValue.ToUnixTimeMilliseconds())
        {
            return DateTimeOffset.MinValue;
        }
        return DateTimeOffset.FromUnixTimeMilliseconds((long)ms);
    }

    private static JsonDocument? DecodeJsonObject(string segment)
    {
        var bytes = TryDecodeBase64Url(segment);
        if (bytes is null)
        {
            return null;
        }
        try
        {
            // Invalid UTF-8 becomes U+FFFD, as in the reference implementation.
            var json = Encoding.UTF8.GetString(bytes);
            var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                return doc;
            }
            doc.Dispose();
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // Segments are checked against [A-Za-z0-9_-]* before decoding. Not a Regex: .NET's "$" also matches
    // before a trailing newline.
    private static bool IsBase64UrlSegment(string segment)
    {
        foreach (var c in segment)
        {
            if (!(c is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '_'))
            {
                return false;
            }
        }
        return true;
    }

    // Unpadded base64url. Like Node's decoder, a dangling final character (length % 4 == 1) carries no whole
    // byte and is ignored.
    private static byte[]? TryDecodeBase64Url(string segment)
    {
        var s = segment.Length % 4 == 1 ? segment[..^1] : segment;
        var b = new StringBuilder(s.Length + 3);
        b.Append(s).Replace('-', '+').Replace('_', '/');
        b.Append('=', (4 - b.Length % 4) % 4);
        try
        {
            return Convert.FromBase64String(b.ToString());
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static string Base64UrlEncode(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    // JavaScript's String.prototype.trim also strips U+FEFF (a byte-order mark), which char.IsWhiteSpace does not.
    private static string TrimJs(string s)
    {
        int start = 0, end = s.Length;
        while (start < end && IsJsWhiteSpace(s[start])) start++;
        while (end > start && IsJsWhiteSpace(s[end - 1])) end--;
        return s[start..end];
    }

    private static bool IsJsWhiteSpace(char c) => c == '﻿' || (char.IsWhiteSpace(c) && c != '\u0085');
}
