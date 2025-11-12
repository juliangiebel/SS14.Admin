using System.Net;
using System.Text.RegularExpressions;
using SS14.Admin.Models;

namespace SS14.Admin.Services;

/// <summary>
/// Implementation of IPiiRedactor that provides deterministic PII redaction.
/// All redaction follows privacy policy guidelines with asterisk notation.
/// </summary>
public class PiiRedactor : IPiiRedactor
{
    private static readonly Regex EmailRegex = new(@"^([^@]+)@(.+)$", RegexOptions.Compiled);
    private static readonly Regex PhoneRegex = new(@"\d", RegexOptions.Compiled);

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

    public string RedactEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return string.Empty;

        var match = EmailRegex.Match(email);
        if (!match.Success || match.Groups.Count < 3)
        {
            // Invalid email format, full redaction
            return new string('*', email.Length);
        }

        var localPart = match.Groups[1].Value;
        var domain = match.Groups[2].Value;

        if (string.IsNullOrEmpty(localPart))
            return new string('*', email.Length);

        // Show first character of local part
        var firstChar = localPart[0];
        return $"{firstChar}***@{domain}";
    }

    public string RedactPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return string.Empty;

        // Extract just the digits
        var digits = PhoneRegex.Matches(phoneNumber)
            .Select(m => m.Value)
            .ToList();

        if (digits.Count < 4)
        {
            // Not enough digits, full redaction
            return new string('*', phoneNumber.Length);
        }

        // Get last 4 digits
        var lastFour = string.Join("", digits.TakeLast(4));

        // Return formatted with last 4 digits visible
        return $"(***) ***-{lastFour}";
    }

    public string RedactPhysicalAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return string.Empty;

        // Simple heuristic: look for city, state pattern
        // This is a basic implementation; more sophisticated parsing could be added
        var parts = address.Split(',').Select(p => p.Trim()).ToList();

        if (parts.Count >= 2)
        {
            // Try to extract what might be city and state (last two parts before zip)
            var potentialState = parts[^1];
            var potentialCity = parts[^2];

            // Very basic state detection (2-letter codes or common state names)
            if (potentialState.Length == 2 || potentialState.Length == 5 || // Could be "IL" or "62701"
                (potentialCity.Length > 2 && potentialCity.Length < 30))     // Reasonable city name
            {
                // If the last part looks like a zip code, use the previous two parts
                if (Regex.IsMatch(potentialState, @"^\d{5}(-\d{4})?$") && parts.Count >= 3)
                {
                    return $"{parts[^3]}, {parts[^2]}";
                }

                return $"{potentialCity}, {potentialState}";
            }
        }

        // If we can't parse it meaningfully, full redaction
        return "***";
    }

    public string RedactValue(string value, PiiKind kind)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return kind switch
        {
            PiiKind.IPv4Address => RedactIPv4(value),
            PiiKind.IPv6Address => RedactIPv6(value),
            PiiKind.HardwareId => RedactHardwareId(value),
            PiiKind.Email => RedactEmail(value),
            PiiKind.PhoneNumber => RedactPhoneNumber(value),
            PiiKind.PhysicalAddress => RedactPhysicalAddress(value),
            PiiKind.Username => RedactUsername(value),
            PiiKind.Generic => new string('*', value.Length),
            _ => new string('*', value.Length)
        };
    }

    /// <summary>
    /// Redacts a username to show only first and last character.
    /// Example: "john_doe" -> "j******e"
    /// </summary>
    private string RedactUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return string.Empty;

        if (username.Length <= 2)
            return new string('*', username.Length);

        var first = username[0];
        var last = username[^1];
        var middleLength = username.Length - 2;

        return $"{first}{new string('*', middleLength)}{last}";
    }
}
