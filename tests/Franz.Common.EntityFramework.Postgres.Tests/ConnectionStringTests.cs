using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.EntityFramework.Postgres.Tests;

public class ConnectionStringTests : IClassFixture<PostgresFixture>
{
  private readonly PostgresFixture _fixture;

  public ConnectionStringTests(PostgresFixture fixture) => _fixture = fixture;

  [Fact]
  public void AddPostgresDatabase_WithDomainId_ShouldReplacePattern()
  {
    // Arrange
    var domainId = Guid.NewGuid().ToString();
    var sp = _fixture.BuildServiceProvider(domainId);

    // Act
    var db = sp.GetRequiredService<TestDbContext>();
    var connectionString = db.Database.GetConnectionString();

    // Assert
    connectionString.Should().Contain($"Database=test_{domainId}");
  }

  [Fact]
  public void AddPostgresDatabase_WithoutDomainId_ShouldRemovePattern()
  {
    // Arrange
    var sp = _fixture.BuildServiceProvider(null);

    // Act
    var db = sp.GetRequiredService<TestDbContext>();
    var connectionString = db.Database.GetConnectionString();

    // Assert
    connectionString.Should().Contain("Database=test");
    connectionString.Should().NotContain("{dbName}");
  }
}
