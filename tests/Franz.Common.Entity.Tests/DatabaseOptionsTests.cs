using Franz.Common.EntityFramework.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.EntityFramework.Tests;

public class DatabaseOptionsTests
{
  [Fact]
  public void SettingValidDatabaseName_ShouldSucceed()
  {
    var options = new DatabaseOptions();
    options.DatabaseName = "TestDb";

    Assert.Equal("TestDb", options.DatabaseName);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void SettingInvalidDatabaseName_ShouldThrow(string invalidName)
  {
    var options = new DatabaseOptions();
    var ex = Assert.Throws<ArgumentException>(() => options.DatabaseName = invalidName);
    Assert.Contains("DatabaseName cannot be null or empty", ex.Message);
  }

  [Fact]
  public void ValidateConnectionSettings_WithValidProperties_ShouldNotThrow()
  {
    var options = new DatabaseOptions
    {
      DatabaseName = "TestDb",
      ServerName = "localhost"
    };

    Exception? ex = Record.Exception(() => options.ValidateConnectionSettings());
    Assert.Null(ex);
  }

  [Fact]
  public void ValidateConnectionSettings_WithMissingDatabaseName_ShouldThrow()
  {
    var options = new DatabaseOptions
    {
      ServerName = "localhost"
    };

    var ex = Assert.Throws<InvalidOperationException>(() => options.ValidateConnectionSettings());
    Assert.Equal("DatabaseName must be specified.", ex.Message);
  }

  [Fact]
  public void ValidateConnectionSettings_WithMissingServerName_ShouldThrow()
  {
    var options = new DatabaseOptions
    {
      DatabaseName = "TestDb"
    };

    var ex = Assert.Throws<InvalidOperationException>(() => options.ValidateConnectionSettings());
    Assert.Equal("ServerName should be provided.", ex.Message);
  }

  [Fact]
  public void NullableProperties_CanBeSetAndRetrieved()
  {
    var options = new DatabaseOptions
    {
      ServerName = "localhost",
      UserName = "user",
      Password = "pass",
      Port = 5432,
      SslMode = "Require",
      DatabaseName = "TestDb"
    };

    Assert.Equal("localhost", options.ServerName);
    Assert.Equal("user", options.UserName);
    Assert.Equal("pass", options.Password);
    Assert.Equal(5432u, options.Port);
    Assert.Equal("Require", options.SslMode);
    Assert.Equal("TestDb", options.DatabaseName);
  }
}
