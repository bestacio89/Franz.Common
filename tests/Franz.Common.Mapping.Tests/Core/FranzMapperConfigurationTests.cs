using FluentAssertions;
using Franz.Common.Mapping.Core;
using Xunit;

namespace Franz.Common.Mapping.Tests.Core;

public class MappingConfigurationTests
{
  // -----------------------------------------------------
  // 1. REGISTER + RETRIEVE
  // -----------------------------------------------------
  [Fact]
  public void Should_Register_And_Retrieve_Mapping()
  {
    var config = new MappingConfiguration();

    var expr = new MappingExpression<User, UserDto>();

    config.Register(expr);

    config.TryGetMapping<User, UserDto>(out var result)
        .Should().BeTrue();

    result.Should().BeSameAs(expr);
  }

  // -----------------------------------------------------
  // 2. LAST WRITE WINS
  // -----------------------------------------------------
  [Fact]
  public void Should_Override_Previous_Mapping()
  {
    var config = new MappingConfiguration();

    var expr1 = new MappingExpression<User, UserDto>();
    var expr2 = new MappingExpression<User, UserDto>();

    config.Register(expr1);
    config.Register(expr2);

    config.TryGetMapping<User, UserDto>(out var result)
        .Should().BeTrue();

    result.Should().BeSameAs(expr2);
  }

  // -----------------------------------------------------
  // 3. TYPE SAFETY
  // -----------------------------------------------------
  [Fact]
  public void Should_Return_False_For_Different_Types()
  {
    var config = new MappingConfiguration();

    config.Register(new MappingExpression<User, UserDto>());

    config.TryGetMapping<Product, ProductDto>(out _)
        .Should().BeFalse();
  }

  // -----------------------------------------------------
  // 4. RUNTIME LOOKUP (NON-GENERIC)
  // -----------------------------------------------------
  [Fact]
  public void Should_Support_Runtime_Type_Lookup()
  {
    var config = new MappingConfiguration();

    var expr = new MappingExpression<User, UserDto>();

    config.Register(expr);

    var found = config.TryGetMapping(
        typeof(User),
        typeof(UserDto),
        out var result);

    found.Should().BeTrue();
    result.Should().BeSameAs(expr);
  }

  // -----------------------------------------------------
  // 5. HAS MAPPING CHECK
  // -----------------------------------------------------
  [Fact]
  public void Should_Report_Mapping_Existence()
  {
    var config = new MappingConfiguration();

    config.Register(new MappingExpression<User, UserDto>());

    config.HasMapping(typeof(User), typeof(UserDto))
        .Should().BeTrue();
  }

  // -----------------------------------------------------
  // 6. EMPTY CONFIG BEHAVIOR
  // -----------------------------------------------------
  [Fact]
  public void Should_Return_False_When_No_Mappings()
  {
    var config = new MappingConfiguration();

    config.TryGetMapping<User, UserDto>(out _)
        .Should().BeFalse();
  }
}