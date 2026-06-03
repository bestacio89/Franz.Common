using FluentAssertions;
using Franz.Common.Errors;
using Franz.Common.Mapping.Core;
using Xunit;

namespace Franz.Common.Mapping.Tests.Core;

public class ImmutableMappingTests
{
  // =========================================================
  // TEST MODELS (THE HAZARD BOUNDARY)
  // =========================================================
  public record PositionalRecordDto(string Name, int Age);

  public class InitOnlyClassDto
  {
    public string Name { get; init; } = string.Empty;
    public int Age { get; init; }
  }

  public class StandardSource
  {
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
  }

  // =========================================================
  // 1. POSITION RECORD TEST (CONGIGURED)
  // =========================================================
  [Fact]
  public void Should_Map_Class_To_Positional_Record_When_Configured()
  {
    var config = new MappingConfiguration();
    config.Register(new MappingExpression<StandardSource, PositionalRecordDto>());

    var mapper = new FranzMapper(config);
    var source = new StandardSource { Name = "Bernardo", Age = 36 };

    // Act
    var result = mapper.Map<StandardSource, PositionalRecordDto>(source);

    // Assert
    result.Name.Should().Be("Bernardo");
    result.Age.Should().Be(36);
  }

  // =========================================================
  // 2. INIT-ONLY PROPERTIES TEST (CONFIGURED)
  // =========================================================
  [Fact]
  public void Should_Map_Init_Only_Properties_Via_Constructor_Binding()
  {
    var config = new MappingConfiguration();

    config.Register(new MappingExpression<StandardSource, InitOnlyClassDto>());

    var mapper = new FranzMapper(config);
    var source = new StandardSource { Name = "Estacio", Age = 36 };

    // Act
    var result = mapper.Map<StandardSource, InitOnlyClassDto>(source);

    // Assert
    result.Should().NotBeNull();

    // We assert behavior, not mechanism:
    result.Name.Should().Be("Estacio");
    result.Age.Should().Be(36);
  }

  // =========================================================
  // 3. FALLBACK PATH FOR RECORDS (NO PROFILE)
  // =========================================================
  [Fact]
  public void Should_Fallback_And_Map_To_Positional_Record_Without_Profile()
  {
    var config = new MappingConfiguration(); // Empty configuration
    var mapper = new FranzMapper(config);
    var source = new StandardSource { Name = "Abreu", Age = 36 };

    // Act
    var result = mapper.Map<StandardSource, PositionalRecordDto>(source);

    // Assert
    result.Name.Should().Be("Abreu");
    result.Age.Should().Be(36);
  }
}