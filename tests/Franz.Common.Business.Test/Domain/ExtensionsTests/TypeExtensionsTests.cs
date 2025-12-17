using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Business.Tests.Domain.ExtensionsTests;

using FluentAssertions;
using Franz.Common.Business.Domain;
using Franz.Common.Business.Extensions;
using Xunit;

public sealed class TypeExtensionsTests
{
  [Fact]
  public void IsEnumerationClass_Should_Return_True_For_Enumeration_Type()
  {
    var result = typeof(TestEnumeration).IsEnumerationClass();

    result.Should().BeTrue();
  }

  [Fact]
  public void IsEnumerationClass_Should_Return_False_For_NonEnumeration_Type()
  {
    var result = typeof(NotAnEnumeration).IsEnumerationClass();

    result.Should().BeFalse();
  }

  [Fact]
  public void IsEnumerationClass_With_Out_GenericType_Should_Output_Generic_Enumeration_Type()
  {
    var result = typeof(TestEnumeration).IsEnumerationClass(out var genericType);

    result.Should().BeTrue();
    genericType.Should().NotBeNull();
    genericType!.GetGenericTypeDefinition().Should().Be(typeof(Enumeration<>));
  }

  [Fact]
  public void IsEnumerationClass_Should_Work_Through_Inheritance()
  {
    var result = typeof(DerivedEnumeration).IsEnumerationClass(out var genericType);

    result.Should().BeTrue();
    genericType.Should().NotBeNull();
    genericType!.GetGenericTypeDefinition().Should().Be(typeof(Enumeration<>));
  }

  [Fact]
  public void IsEnumerationClass_Should_Return_False_And_Null_Generic_For_Invalid_Type()
  {
    var result = typeof(NotAnEnumeration).IsEnumerationClass(out var genericType);

    result.Should().BeFalse();
    genericType.Should().BeNull();
  }
}
