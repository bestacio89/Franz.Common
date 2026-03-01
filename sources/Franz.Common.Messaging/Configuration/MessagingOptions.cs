#nullable enable
namespace Franz.Common.Messaging.Configuration;

public class MessagingOptions
{
  public string? HostName { get; set; }
  public string? UserName { get; set; }
  public string? Password { get; set; }
  public string? VirtualHost { get; set; }
  public bool? SslEnabled { get; set; }
  public string GroupID { get; set; } = string.Empty;
  public string BootStrapServers { get; set; } = "localhost:9092";
  public int? Port { get; set; }
  public string SslCaLocation { get; set; } = string.Empty;
  public string SslCertificateLocation { get; set; } = string.Empty;
  public string SslKeyLocation { get; set; } = string.Empty;
}