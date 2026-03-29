#nullable enable
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Franz.Common.Caching.Tests.Fakes;

/// <summary>
/// A high-fidelity test payload for verifying caching serialization, 
/// required member validation, and AOT compatibility.
/// </summary>
public sealed record TestPayload
{
  /// <summary>
  /// Parameterless constructor with [SetsRequiredMembers] to satisfy 
  /// C# required member constraints during manual instantiation or deserialization.
  /// </summary>
  [SetsRequiredMembers]
  public TestPayload() { }

  /// <summary>
  /// Primary-style constructor for quick "Arrange" setups in unit tests.
  /// </summary>
  [SetsRequiredMembers]
  public TestPayload(Guid id, string name)
  {
    Id = id;
    Name = name;
  }

  [Required]
  public required Guid Id { get; init; } = Guid.CreateVersion7();

  [Required]
  [StringLength(100)]
  public required string Name { get; init; } = string.Empty;

  public string? Description { get; init; }

  public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;


  /// <summary>
  /// Helper to generate a standardized "valid" payload for generic tests.
  /// </summary>
  public static TestPayload CreateDefault() => new(Guid.CreateVersion7(), "Franz-Test-Artifact")
  {
    Description = "Automated test payload for distributed caching verification.",
   
  };
}