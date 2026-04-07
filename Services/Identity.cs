using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace KeyMint.Services;

/// <summary>
/// Machine identity utilities for the KeyMint C# SDK.
/// Provides methods for hardware fingerprinting and persistent installation IDs.
/// </summary>
public static class KeyMintIdentity
{
    // ─── Garbage Detection ──────────────────────────────────────────────

    private static readonly string[] GarbageStrings =
    {
        "ffffffffffffffffffffffffffffffff",
        "03000200040005000006000700080009",
        "defaultstring",
        "tobefilledbyoem",
        "notapplicable",
        "notspecified",
        "systemserialnum",
        "none",
    };

    private static readonly Regex[] GarbageRegexes =
    {
        new(@"^0+$", RegexOptions.Compiled),
        new(@"^f+$", RegexOptions.Compiled),
    };

    private static readonly Regex NormalizeRegex = new(@"[-:\s._]", RegexOptions.Compiled);

    private static bool IsGarbageId(string id)
    {
        var normalized = NormalizeRegex.Replace(id.ToLowerInvariant(), "");
        foreach (var regex in GarbageRegexes)
        {
            if (regex.IsMatch(normalized)) return true;
        }
        foreach (var garbage in GarbageStrings)
        {
            if (normalized == garbage || normalized.Contains(garbage)) return true;
        }
        return false;
    }

    private static string HashId(string raw)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw.ToLowerInvariant().Trim()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    // ─── Fingerprint Layers ─────────────────────────────────────────────

    /// <summary>Layer 1: BIOS / Hardware UUID</summary>
    private static string? GetBiosUUID()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return RunCommand("powershell.exe",
                    "-Command \"(Get-CimInstance Win32_ComputerSystemProduct).UUID\"");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return RunCommand("bash",
                    "-c \"ioreg -rd1 -c IOPlatformExpertDevice | grep IOPlatformUUID | awk -F'\\\"' '{print $4}'\"");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var path = "/sys/class/dmi/id/product_uuid";
                if (File.Exists(path))
                    return File.ReadAllText(path).Trim();
            }
        }
        catch { /* fall through */ }
        return null;
    }

    /// <summary>Layer 2: OS-level persistent machine ID</summary>
    private static string? GetOSMachineId()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return RunCommand("powershell.exe",
                    "-Command \"(Get-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Cryptography' -Name MachineGuid).MachineGuid\"");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return RunCommand("bash",
                    "-c \"ioreg -rd1 -c IOPlatformExpertDevice | grep IOPlatformSerialNumber | awk -F'\\\"' '{print $4}'\"");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                foreach (var path in new[] { "/etc/machine-id", "/var/lib/dbus/machine-id" })
                {
                    if (File.Exists(path))
                    {
                        var value = File.ReadAllText(path).Trim();
                        if (!string.IsNullOrEmpty(value))
                            return value;
                    }
                }
            }
        }
        catch { /* fall through */ }
        return null;
    }

    /// <summary>Layer 3: Primary network interface MAC address</summary>
    private static string? GetPrimaryMAC()
    {
        try
        {
            var nic = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up
                         && n.NetworkInterfaceType != NetworkInterfaceType.Loopback
                         && !n.Name.StartsWith("veth", StringComparison.OrdinalIgnoreCase)
                         && !n.Name.StartsWith("docker", StringComparison.OrdinalIgnoreCase)
                         && !n.Name.StartsWith("br-", StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            if (nic != null)
            {
                var mac = nic.GetPhysicalAddress().ToString();
                if (!string.IsNullOrEmpty(mac) && mac != "000000000000")
                {
                    // Format as XX:XX:XX:XX:XX:XX
                    return string.Join(":", Enumerable.Range(0, 6).Select(i => mac.Substring(i * 2, 2)));
                }
            }
        }
        catch { /* fall through */ }
        return null;
    }

    /// <summary>Runs a shell command and returns trimmed stdout.</summary>
    private static string? RunCommand(string fileName, string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi);
            if (process == null) return null;

            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(5000);

            return string.IsNullOrEmpty(output) ? null : output;
        }
        catch
        {
            return null;
        }
    }

    // ─── Public API ─────────────────────────────────────────────────────

    /// <summary>
    /// Best-effort hardware fingerprint. Attempts to read the machine's
    /// BIOS/System UUID, then falls back through OS-level IDs and network
    /// interfaces. May return different values after hardware changes or
    /// OS reinstalls.
    /// <para>
    /// Use this for logging, display, or secondary validation.
    /// For activation HostId, prefer <see cref="GetOrCreateInstallationId"/>.
    /// </para>
    /// </summary>
    /// <returns>A SHA-256 hashed 64-character hex string, or null if every layer failed.</returns>
    public static string? GetMachineId()
    {
        Func<string?>[] layers = { GetBiosUUID, GetOSMachineId, GetPrimaryMAC };

        foreach (var layer in layers)
        {
            try
            {
                var raw = layer();
                if (!string.IsNullOrEmpty(raw) && raw.Length > 4 && !IsGarbageId(raw))
                {
                    return HashId(raw);
                }
            }
            catch
            {
                continue;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns a guaranteed-unique, guaranteed-stable installation identifier.
    /// On first call, generates a UUIDv4 seeded with whatever hardware info
    /// is available and persists it to disk. Every subsequent call returns the
    /// same value.
    /// <para>
    /// This is the <b>recommended</b> value to pass as <c>hostId</c> when
    /// activating a license key.
    /// </para>
    /// </summary>
    /// <param name="storagePath">
    /// Optional custom path for the persistence file.
    /// Defaults to ~/.keymint/installation-id.
    /// </param>
    /// <returns>A SHA-256 hashed 64-character hex string.</returns>
    /// <exception cref="IOException">If the file cannot be read or written.</exception>
    public static string GetOrCreateInstallationId(string? storagePath = null)
    {
        var filePath = storagePath ??
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".keymint", "installation-id");

        // 1. If the file exists, trust it
        if (File.Exists(filePath))
        {
            var stored = File.ReadAllText(filePath).Trim();
            if (!string.IsNullOrEmpty(stored))
            {
                var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(stored));
                return Convert.ToHexString(bytes).ToLowerInvariant();
            }
        }

        // 2. Generate a new installation ID, anchored to hardware when possible
        var hardwareAnchor = GetMachineId() ?? "";
        var newUuid = Guid.NewGuid().ToString();
        var compositeId = $"{newUuid}:{hardwareAnchor}:{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

        // 3. Persist it
        var dir = Path.GetDirectoryName(filePath)!;
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        File.WriteAllText(filePath, compositeId);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(compositeId));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
