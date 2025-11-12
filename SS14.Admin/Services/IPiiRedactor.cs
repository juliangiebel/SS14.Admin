namespace SS14.Admin.Services;

/// <summary>
/// Service for redacting personally identifiable information (PII).
/// All redaction is deterministic and follows privacy policy guidelines.
/// </summary>
public interface IPiiRedactor
{
    /// <summary>
    /// Redacts an IPv4 address to show only the first octet.
    /// Example: "203.0.113.42" -> "203.*.*.*"
    /// </summary>
    string RedactIPv4(string ipv4Address);

    /// <summary>
    /// Redacts an IPv6 address to show only the first hextet.
    /// Example: "2001:0db8:85a3::8a2e:0370:7334" -> "2001:*:*:*:*:*:*:*"
    /// </summary>
    string RedactIPv6(string ipv6Address);

    /// <summary>
    /// Redacts a hardware ID to show first 8 and last 4 characters.
    /// Example: "a1b2c3d4e5f6g7h8" -> "a1b2c3d4...g7h8"
    /// </summary>
    string RedactHardwareId(string hardwareId);
}
