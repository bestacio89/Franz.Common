using Franz.Common.Business.Tests.Samples;
using Xunit;

namespace Franz.Common.Business.Tests.Domain;

public class EntityTest
{

  [Fact]
  public void Equals_DifferentObjects_ReturnsFalse()
  {
    var instanceA = new EntitySample(1);
    var instanceB = new EntitySample(2);

    var actual = instanceA.Equals(instanceB);

    Assert.False(actual);
  }

  [Fact]
  public void Equals_DifferentObjectsWithSameId_ReturnsTrue()
  {
    var instanceA = new EntitySample(1);
    var instanceB = new EntitySample(1);

    var actual = instanceA.Equals(instanceB);

    Assert.True(actual);
  }

  [Fact]
  public void Equals_DifferentObjectsWithSameIdTypeOfGuid_ReturnsTrue()
  {
    var id = Guid.NewGuid();
    var instanceA = new EntitySample<Guid>(id);
    var instanceB = new EntitySample<Guid>(id);

    var actual = instanceA.Equals(instanceB);

    Assert.True(actual);
  }

  [Fact]
  public void OperatorEquals_DifferentObjectsWithSameId_ReturnsTrue()
  {
    var instanceA = new EntitySample(1);
    var instanceB = new EntitySample(1);

    var actual = instanceA == instanceB;

    Assert.True(actual);
  }

  [Fact]
  public void OperatorNotEquals_DifferentObjectsWithSameId_ReturnsTrue()
  {
    var instanceA = new EntitySample(1);
    var instanceB = new EntitySample(1);

    var actual = instanceA != instanceB;

    Assert.False(actual);
  }
}
