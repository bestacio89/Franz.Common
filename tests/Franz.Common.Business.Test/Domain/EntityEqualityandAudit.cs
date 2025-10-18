using Franz.Common.Business.Domain;
using Xunit;
using FluentAssertions;

public class EntityEqualityTests
{
  private sealed class TestEntity : Entity<Guid>
  {
    public TestEntity(Guid id) => Id = id;
  }

  [Fact]
  public void Entities_WithSameId_AreEqual()
  {
    var id = Guid.NewGuid();
    var e1 = new TestEntity(id);
    var e2 = new TestEntity(id);

    e1.Should().Be(e2);
  }

  [Fact]
  public void MarkCreated_SetsCreatedBy_AndDateCreated()
  {
    var entity = new TestEntity(Guid.NewGuid());
    entity.MarkCreated("tester");

    entity.CreatedBy.Should().Be("tester");
    entity.DateCreated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
  }
}
