using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Franz.Common.EntityFramework.Oracle.Tests;

public class OraclePersistenceTests : IClassFixture<OracleFixture>
{
  private readonly OracleFixture _fixture;

  public OraclePersistenceTests(OracleFixture fixture) => _fixture = fixture;

  [Fact]
  public async Task Should_Register_Oracle_Context_And_Connect()
  {
    // Arrange
    var sp = _fixture.BuildServiceProvider();
    using var scope = sp.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();

    // Act & Assert
    var canConnect = await db.Database.CanConnectAsync();
    canConnect.Should().BeTrue();
  }

  // Add additional persistence/soft-delete tests here as you harden the provider
}