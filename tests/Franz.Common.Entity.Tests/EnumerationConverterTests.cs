using Franz.Common.Business.Domain;
using Franz.Common.EntityFramework;
using System;
using System.Reflection;
using Xunit;

namespace Franz.Common.EntityFramework.Tests
{
  // Dummy Enumeration for testing
  public class TestEnum : Enumeration<int>
  {
    public static readonly TestEnum One = new(1, "One");
    public static readonly TestEnum Two = new(2, "Two");

    public TestEnum(int id, string name) : base(id, name) { }
  }

  public class EnumerationConverterTests
  {
    [Fact]
    public void ConvertToProvider_Returns_CorrectId()
    {
      // Arrange
      var converter = new EnumerationConverter<TestEnum, int>();

      // Act
      var id = converter.ConvertToProviderExpression.Compile()(TestEnum.Two);

      // Assert
      Assert.Equal(2, id);
    }

    [Fact]
    public void ConvertFromProvider_Returns_CorrectEnumeration()
    {
      // Arrange
      var converter = new EnumerationConverter<TestEnum, int>();

      // Act
      var enumeration = converter.ConvertFromProviderExpression.Compile()(1);

      // Assert
      Assert.Equal(TestEnum.One, enumeration);
    }

    [Fact]
    public void ConvertToProvider_Throws_OnNullEnumeration()
    {
      // Arrange
      var converter = new EnumerationConverter<TestEnum, int>();

      // Act & Assert
      var ex = Assert.Throws<NullReferenceException>(() =>
          converter.ConvertToProviderExpression.Compile()(null!)
      );
      Assert.Contains("not set to", ex.Message);
    }
    [Fact]
    public void ConvertFromProvider_Throws_OnNullId()
    {
      // Keep the types as they are in production (int, not int?)
      var converter = new EnumerationConverter<TestEnum, int>();

      // Compile to a Delegate so we can pass whatever we want to it
      Delegate del = converter.ConvertFromProviderExpression.Compile();

      // Use DynamicInvoke to bypass the 'int' requirement and pass 'null'
      var ex = Assert.Throws<TargetInvocationException>(() =>
          del.DynamicInvoke(new object[] { null! })
      );

      // The logic inside the converter will throw ArgumentNullException,
      // but Reflection wraps it in a TargetInvocationException.
      Assert.IsType<InvalidOperationException>(ex.InnerException);
      Assert.Contains("is not a valid value", ex.InnerException.Message);
    }
  }
}