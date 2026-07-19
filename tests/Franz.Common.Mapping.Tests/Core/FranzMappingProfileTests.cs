using FluentAssertions;
using Franz.Common.Errors;
using Franz.Common.Mapping.Core;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace Franz.Common.Mapping.Tests.Core;

public class FranzMapperTests
{
  // -----------------------------------------------------
  // 1. BASIC MAPPING
  // -----------------------------------------------------
  [Fact]
  public void Should_Map_Basic_Properties()
  {
    var config = new MappingConfiguration();

    config.Register(
        new MappingExpression<User, UserDto>()
    );

    var mapper = new FranzMapper(config);

    var result = mapper.Map<User, UserDto>(
        new User { Name = "Bernardo" });

    result.Name.Should().Be("Bernardo");
  }

  // -----------------------------------------------------
  // 2. FALLBACK MAPPING
  // -----------------------------------------------------
  [Fact]
  public void Should_Fallback_When_No_Profile()
  {
    var config = new MappingConfiguration();

    var mapper = new FranzMapper(config);

    var result = mapper.Map<Product, ProductDto>(
        new Product { Price = 50m });

    result.Price.Should().Be(50m);
  }

  // -----------------------------------------------------
  // 3. CONSTRUCTOR MAPPING
  // -----------------------------------------------------
  [Fact]
  public void Should_Use_ConstructUsing()
  {
    var config = new MappingConfiguration();

    config.Register(
        new MappingExpression<User, UserDto>()
            .ConstructUsing(u => new UserDto
            {
              Name = "X-" + u.Name
            })
    );

    var mapper = new FranzMapper(config);

    var result = mapper.Map<User, UserDto>(
        new User { Name = "Test" });

    result.Name.Should().Be("X-Test");
  }

  // -----------------------------------------------------
  // 4. COLLECTION MAPPING
  // -----------------------------------------------------
  [Fact]
  public void Should_Map_Collections()
  {
    var config = new MappingConfiguration();

    config.Register(new MappingExpression<User, UserDto>());

    var mapper = new FranzMapper(config);

    var result = mapper.Map<List<User>, List<UserDto>>(
        new List<User>
        {
            new User { Name = "A" },
            new User { Name = "B" }
        });

    result.Should().HaveCount(2);
    result[0].Name.Should().Be("A");
    result[1].Name.Should().Be("B");
  }

  // -----------------------------------------------------
  // 5. NESTED MAPPING
  // -----------------------------------------------------
  [Fact]
  public void Should_Map_Nested_Objects()
  {
    var config = new MappingConfiguration();

    config.Register(new MappingExpression<Address, AddressDto>());
    config.Register(new MappingExpression<User, UserDto>());

    var mapper = new FranzMapper(config);

    var result = mapper.Map<User, UserDto>(
        new User
        {
          Address = new Address { City = "Paris" }
        });

    result.Address.City.Should().Be("Paris");
  }

  // -----------------------------------------------------
  // 6. CIRCULAR DETECTION
  // -----------------------------------------------------
  [Fact]
  public void Should_Detect_Circular_Reference()
  {
    var config = new MappingConfiguration();

    config.Register(new MappingExpression<Node, NodeDto>());

    var mapper = new FranzMapper(config);

    var node = new Node();
    node.Child = node;

    var act = () => mapper.Map<Node, NodeDto>(node);

    act.Should()
       .Throw<TechnicalException>()
       .WithMessage("[FranzMapper] Circular reference detected while mapping 'Franz.Common.Mapping.Tests.Core.Node'.");
  }

  // -----------------------------------------------------
  // 7. VALUE UNWRAPPING
  // -----------------------------------------------------
  [Fact]
  public void Should_Unwrap_Value_Property()
  {
    var config = new MappingConfiguration();

    config.Register(new MappingExpression<WrappedInt, int>());

    var mapper = new FranzMapper(config);

    var result = mapper.Map<WrappedInt, int>(
        new WrappedInt { Value = 42 });

    result.Should().Be(42);
  }
}