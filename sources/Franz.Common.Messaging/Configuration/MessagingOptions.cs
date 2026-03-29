#nullable enable
namespace Franz.Common.Messaging.Configuration;

/// <summary>
/// Base options shared between all messaging protocols.
/// </summary>
public class MessagingOptions
{
  // --- Connectivity (Shared) ---
  public string? HostName { get; set; }
  public string? UserName { get; set; }
  public string? Password { get; set; }
  public string? VirtualHost { get; set; }
  public int? Port { get; set; }

  // --- Security (Shared) ---
  public bool? SslEnabled { get; set; }
  public string? SslCaLocation { get; set; }
  public string? SslCertificateLocation { get; set; }
  public string? SslKeyLocation { get; set; }

  // --- Publisher options (shared) ---
  public int PublisherConfirmTimeoutSeconds { get; set; } = 5;
}