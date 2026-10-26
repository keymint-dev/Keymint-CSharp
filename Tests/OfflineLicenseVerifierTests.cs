using System.Security.Cryptography;
using System.Text.Json;
using KeyMint.Services;
using Xunit;

namespace KeyMint.Tests;

/// <summary>
/// Runs the shared Keymint offline-license test vectors (fixtures/offline-license-fixtures.json, copied unchanged
/// from keymint-codebase scripts/fixtures) and checks argument handling.
/// </summary>
public class OfflineLicenseVerifierTests
{
    private static readonly Lazy<JsonDocument> Fixtures = new(() =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures", "offline-license-fixtures.json"))));

    private static JsonElement Root => Fixtures.Value.RootElement;

    private static string[] TrustedKeys => Root.GetProperty("publicKeys").EnumerateArray().Select(k => k.GetString()!).ToArray();

    public static IEnumerable<object[]> CaseNames() =>
        Root.GetProperty("cases").EnumerateArray().Select(c => new object[] { c.GetProperty("name").GetString()! });

    private static JsonElement Case(string name) =>
        Root.GetProperty("cases").EnumerateArray().Single(c => c.GetProperty("name").GetString() == name);

    private static OfflineVerifyOptions ValidOptions() => new()
    {
        PublicKeys = TrustedKeys,
        ProductId = "prod_7Hq2LmX9vT4kPz1aB3cD",
        MachineCode = "machine-3f9a2c7e",
        Now = DateTimeOffset.FromUnixTimeSeconds(1800000000),
    };

    private static string ValidLicFile => Case("valid .lic file").GetProperty("input").GetString()!;

    [Fact]
    public void Fixture_file_has_all_34_cases()
    {
        Assert.Equal(34, Root.GetProperty("cases").GetArrayLength());
    }

    [Theory]
    [MemberData(nameof(CaseNames))]
    public void Matches_shared_fixture(string name)
    {
        var c = Case(name);
        var o = c.GetProperty("options");
        var options = new OfflineVerifyOptions
        {
            PublicKeys = TrustedKeys,
            ProductId = o.GetProperty("productId").GetString()!,
            MachineCode = o.GetProperty("machineCode").GetString()!,
            Now = DateTimeOffset.FromUnixTimeSeconds(o.GetProperty("now").GetInt64()),
        };
        if (o.TryGetProperty("clockSkew", out var skew))
        {
            options.ClockSkew = TimeSpan.FromSeconds(skew.GetDouble());
        }
        var input = c.GetProperty("input").GetString()!;
        var expect = c.GetProperty("expect");

        if (expect.GetProperty("valid").GetBoolean())
        {
            var want = expect.GetProperty("license");
            var got = OfflineLicenseVerifier.Verify(input, options);
            Assert.Equal(want.GetProperty("licenseKey").GetString(), got.LicenseKey);
            Assert.Equal(want.GetProperty("productId").GetString(), got.ProductId);
            Assert.Equal(want.GetProperty("machineCode").GetString(), got.MachineCode);
            Assert.Equal(want.GetProperty("keyId").GetString(), got.KeyId);
            Assert.Equal(want.GetProperty("expiresAt").GetInt64(), got.ExpiresAt.ToUnixTimeSeconds());
            var iat = want.GetProperty("issuedAt");
            Assert.Equal(iat.ValueKind == JsonValueKind.Null ? null : iat.GetInt64(), got.IssuedAt?.ToUnixTimeSeconds());

            Assert.True(OfflineLicenseVerifier.TryVerify(input, options, out var tryResult, out var tryError));
            Assert.Null(tryError);
            Assert.Equal(got.LicenseKey, tryResult!.LicenseKey);
        }
        else
        {
            var wantCode = expect.GetProperty("error").GetString();
            var ex = Assert.Throws<OfflineLicenseException>(() => OfflineLicenseVerifier.Verify(input, options));
            Assert.Equal(wantCode, ex.CodeString);
            Assert.Equal(wantCode, ex.Code.ToSpecString());

            Assert.False(OfflineLicenseVerifier.TryVerify(input, options, out var tryResult, out var tryError));
            Assert.Null(tryResult);
            Assert.Equal(ex.Code, tryError);
        }
    }

    [Fact]
    public void Computes_the_fixture_key_ids()
    {
        var expected = Root.GetProperty("keyIds").EnumerateArray().Select(k => k.GetString()).ToArray();
        Assert.Equal(expected, TrustedKeys.Select(OfflineLicenseVerifier.ComputeKeyId).ToArray());
    }

    [Fact]
    public void Error_messages_never_contain_the_license_key()
    {
        var options = ValidOptions();
        options.ProductId = "prod_other";
        var ex = Assert.Throws<OfflineLicenseException>(() => OfflineLicenseVerifier.Verify(ValidLicFile, options));
        Assert.Equal(OfflineLicenseErrorCode.WrongProduct, ex.Code);
        Assert.DoesNotContain("KM-ABCD-1234-EFGH-5678", ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("KM-ABCD-1234-EFGH-5678", ex.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Spec_strings_cover_every_code()
    {
        var codes = Enum.GetValues<OfflineLicenseErrorCode>().Select(c => c.ToSpecString()).ToArray();
        Assert.Equal(
            new[] { "malformed", "unsupported_algorithm", "unknown_key", "bad_signature", "wrong_type", "wrong_product", "wrong_machine", "expired" },
            codes);
    }

    [Fact]
    public void Defaults_to_the_system_clock_and_60_second_skew()
    {
        var options = ValidOptions();
        options.Now = null;
        Assert.Equal(TimeSpan.FromSeconds(60), options.ClockSkew);
        // The fixture license expires on 2027-02-15; with Now unset the result depends on the system clock.
        var ok = OfflineLicenseVerifier.TryVerify(ValidLicFile, options, out _, out var error);
        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() < 1802592000 + 60)
        {
            Assert.True(ok);
        }
        else
        {
            Assert.Equal(OfflineLicenseErrorCode.Expired, error);
        }
    }

    [Fact]
    public void Clock_skew_of_zero_and_one_hour_are_allowed()
    {
        var options = ValidOptions();
        options.ClockSkew = TimeSpan.Zero;
        OfflineLicenseVerifier.Verify(ValidLicFile, options);
        options.ClockSkew = TimeSpan.FromHours(1);
        OfflineLicenseVerifier.Verify(ValidLicFile, options);
    }

    [Fact]
    public void Rejects_no_public_keys()
    {
        var options = ValidOptions();
        options.PublicKeys = Array.Empty<string>();
        Assert.Throws<ArgumentException>(() => OfflineLicenseVerifier.Verify(ValidLicFile, options));
        Assert.Throws<ArgumentException>(() => OfflineLicenseVerifier.TryVerify(ValidLicFile, options, out _, out _));
    }

    [Theory]
    [InlineData("not a pem")]
    [InlineData("")]
    [InlineData("-----BEGIN PUBLIC KEY-----\nbm90IGEga2V5\n-----END PUBLIC KEY-----\n")]
    [InlineData("-----BEGIN CERTIFICATE-----\nMCowBQYDK2VwAyEAByGIA6xLMplivs4DVsZ5om7vFAIB5bVwAU/XWDDpvpQ=\n-----END CERTIFICATE-----\n")]
    public void Rejects_garbage_public_keys(string pem)
    {
        var options = ValidOptions();
        options.PublicKeys = new[] { pem };
        Assert.Throws<ArgumentException>(() => OfflineLicenseVerifier.Verify(ValidLicFile, options));
    }

    [Fact]
    public void Rejects_a_non_Ed25519_public_key()
    {
        using var rsa = RSA.Create(2048);
        var options = ValidOptions();
        options.PublicKeys = new[] { TrustedKeys[0], rsa.ExportSubjectPublicKeyInfoPem() };
        var ex = Assert.Throws<ArgumentException>(() => OfflineLicenseVerifier.Verify(ValidLicFile, options));
        Assert.Contains("Ed25519", ex.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Rejects_empty_product_or_machine(string? value)
    {
        var options = ValidOptions();
        options.ProductId = value!;
        Assert.Throws<ArgumentException>(() => OfflineLicenseVerifier.Verify(ValidLicFile, options));

        options = ValidOptions();
        options.MachineCode = value!;
        Assert.Throws<ArgumentException>(() => OfflineLicenseVerifier.Verify(ValidLicFile, options));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3601)]
    public void Rejects_clock_skew_out_of_range(int seconds)
    {
        var options = ValidOptions();
        options.ClockSkew = TimeSpan.FromSeconds(seconds);
        Assert.Throws<ArgumentOutOfRangeException>(() => OfflineLicenseVerifier.Verify(ValidLicFile, options));
    }

    [Fact]
    public void Rejects_null_license_and_options()
    {
        Assert.Throws<ArgumentNullException>(() => OfflineLicenseVerifier.Verify(null!, ValidOptions()));
        Assert.Throws<ArgumentNullException>(() => OfflineLicenseVerifier.Verify(ValidLicFile, null!));
    }
}
