using System.Net;

namespace SS14.Admin.Services;

/// <summary>
/// Implementation of IPiiRedactor that provides deterministic PII redaction.
/// All redaction follows privacy policy guidelines with asterisk notation.
/// </summary>
public class PiiRedactor : IPiiRedactor
{
    public string RedactIPv4(string ipv4Address)
    {
        if (string.IsNullOrWhiteSpace(ipv4Address))
            return string.Empty;

        // Try to parse as IPv4
        if (!IPAddress.TryParse(ipv4Address, out var ip) || ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
        {
            // If not a valid IPv4, return full redaction
            return new string('*', ipv4Address.Length);
        }

        var octets = ipv4Address.Split('.');
        if (octets.Length != 4)
            return new string('*', ipv4Address.Length);

        // Show only first octet
        return $"{octets[0]}.*.*.*";
    }

    public string RedactIPv6(string ipv6Address)
    {
        if (string.IsNullOrWhiteSpace(ipv6Address))
            return string.Empty;

        // Try to parse as IPv6
        if (!IPAddress.TryParse(ipv6Address, out var ip) || ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            // If not a valid IPv6, return full redaction
            return new string('*', ipv6Address.Length);
        }

        // Expand the IPv6 to full form, then redact
        var bytes = ip.GetAddressBytes();
        var firstHextet = $"{bytes[0]:x2}{bytes[1]:x2}";

        // Return first hextet only
        return $"{firstHextet}:*:*:*:*:*:*:*";
    }

    public string RedactHardwareId(string hardwareId)
    {
        if (string.IsNullOrWhiteSpace(hardwareId))
            return string.Empty;

        // Remove any hyphens or formatting
        var cleaned = hardwareId.Replace("-", "").Replace(" ", "");

        if (cleaned.Length <= 12)
        {
            // If too short to meaningfully redact, full redaction
            return new string('*', hardwareId.Length);
        }

        // Show first 8 chars, "...", and last 4 chars
        var first = cleaned.Substring(0, 8);
        var last = cleaned.Substring(cleaned.Length - 4);

        return $"{first}...{last}";
    }
}
