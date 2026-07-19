using FluentAssertions;
using Franz.Common.Mapping.Core;
using System.Linq.Expressions;
using Xunit;

namespace Franz.Common.Mapping.Tests.Core;

public class MappingExpressionTests
{
  // =========================================================
  // TEST MODELS
  // =========================================================
  private sealed class SourceModel
  {
    public string Id { get; set; } = string.Empty;
    public int NumericValue { get; set; }
    public string Name { get; set; } = string.Empty;
  }

  private sealed class DestinationModel
  {
    public string DocumentId { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public string IgnoredField { get; set; } = string.Empty;

    public DestinationModel() { }
    public DestinationModel(string documentId) => DocumentId = documentId;
  }

  // =========================================================
  // CONTRACT METADATA TESTS
  // =========================================================
  [Fact]
  public void Should_expose_correct_source_and_destination_types()
  {
    // Arrange & Act
    var expression = new MappingExpression<SourceModel, DestinationModel>();

    // Assert
    expression.SourceType.Should().Be(typeof(SourceModel));
    expression.DestinationType.Should().Be(typeof(DestinationModel));
  }

  // =========================================================
  // FLUENT CONFIGURATION TESTS
  // =========================================================
  [Fact]
  public void Should_enable_strict_mode_when_configured()
  {
    // Arrange
    var expression = new MappingExpression<SourceModel, DestinationModel>();

    // Act
    expression.Strict();

    // Assert
    expression.IsStrict.Should().BeTrue();
  }

  [Fact]
  public void Should_bind_reference_type_member_expressions_correctly()
  {
    // Arrange
    var expression = new MappingExpression<SourceModel, DestinationModel>();

    // Act
    expression.ForMember(dest => dest.DocumentId, src => src.Id);

    // Assert
    expression.MemberBindings.Should().ContainKey("DocumentId")
        .WhoseValue.Should().Be("Id");
  }

  [Fact]
  public void Should_bind_value_type_member_expressions_correctly_by_unwrapping_boxing()
  {
    // Arrange
    var expression = new MappingExpression<SourceModel, DestinationModel>();

    // Act
    // mapping an int to object forces a UnaryExpression (Convert) in the Expression Tree
    expression.ForMember(dest => dest.TotalCount, src => src.NumericValue);

    // Assert
    expression.MemberBindings.Should().ContainKey("TotalCount")
        .WhoseValue.Should().Be("NumericValue");
  }

  [Fact]
  public void Should_track_ignored_members_correctly()
  {
    // Arrange
    var expression = new MappingExpression<SourceModel, DestinationModel>();

    // Act
    expression.Ignore(dest => dest.IgnoredField);

    // Assert
    expression.IgnoredMembers.Should().Contain("IgnoredField")
        .And.HaveCount(1);
  }

  // =========================================================
  // CONSTRUCTOR BINDING TESTS
  // =========================================================
  [Fact]
  public void Should_register_custom_constructor_delegate()
  {
    // Arrange
    var expression = new MappingExpression<SourceModel, DestinationModel>();
    Func<SourceModel, DestinationModel> ctor = src => new DestinationModel(src.Id);

    // Act
    expression.ConstructUsing(ctor);

    // Assert
    expression.HasConstructor.Should().BeTrue();
    expression.Constructor.Should().BeSameAs(ctor);
  }

  [Fact]
  public void Should_register_reverse_constructor_delegate()
  {
    // Arrange
    var expression = new MappingExpression<SourceModel, DestinationModel>();
    Func<DestinationModel, SourceModel> reverseCtor = dest => new SourceModel { Id = dest.DocumentId };

    // Act
    expression.ReverseConstructUsing(reverseCtor);

    // Assert
    expression.ReverseConstructor.Should().BeSameAs(reverseCtor);
  }

  // =========================================================
  // EDGE CASES / VALIDATION TESTS
  // =========================================================
  [Fact]
  public void Should_throw_InvalidOperationException_when_expression_is_not_a_member()
  {
    // Arrange
    var expression = new MappingExpression<SourceModel, DestinationModel>();

    // Act
    var act = () => expression.ForMember(dest => "NotAMember", src => src.Id);

    // Assert
    act.Should()
   .Throw<InvalidOperationException>()
   .Where(ex =>
       ex.Message.Contains(
           "invalid mapping expression",
           StringComparison.OrdinalIgnoreCase)); ;
  }
}