using System;

namespace Franz.Common.EntityFramework.Configuration
{
  public class DatabaseOptions
  {
    private string _databaseName = default!;

    public string? ServerName { get; set; }

    public string DatabaseName
    {
      get => _databaseName;
      set
      {
        if (string.IsNullOrWhiteSpace(value))
          throw new ArgumentException("DatabaseName cannot be null or empty", nameof(DatabaseName));
        _databaseName = value;
      }
    }

    public string? UserName { get; set; }

    public string? Password { get; set; }

    public uint? Port { get; set; }

    public string? SslMode { get; set; }

    /// <summary>
    /// Optional method to validate minimal required properties for connecting
    /// </summary>
    public void ValidateConnectionSettings()
    {
      if (string.IsNullOrWhiteSpace(DatabaseName))
        throw new InvalidOperationException("DatabaseName must be specified.");

      if (string.IsNullOrWhiteSpace(ServerName))
        throw new InvalidOperationException("ServerName should be provided.");

      // Optional: enforce username/password if needed
    }
  }
}