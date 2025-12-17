using Franz.Common.Business.Domain;
using Xunit;
using FluentAssertions;
using Franz.Common.Business.Tests.Domain.EntityTests;

namespace Franz.Common.Business.Test.Domain.EntityTests;

public class EntityEqualityTests
{


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

  [Fact]
  public void IsTransient_Should_Be_True_When_Id_Is_Default()
  {
    var entity = new TestEntity();

    entity.IsTransient().Should().BeTrue();
  }

  [Fact]
  public void IsTransient_Should_Be_False_When_Id_Is_Set()
  {
    var entity = new TestEntity(Guid.NewGuid());

    entity.IsTransient().Should().BeFalse();
  }

  [Fact]
  public void Equals_Should_Be_True_For_Same_Type_And_Id()
  {
    var id = Guid.NewGuid();

    var a = new TestEntity(id);
    var b = new TestEntity(id);

    a.Should().Be(b);
    (a == b).Should().BeTrue();
  }

  [Fact]
  public void Equals_Should_Be_False_For_Transient_Entities()
  {
    var a = new TestEntity();
    var b = new TestEntity();

    a.Should().NotBe(b);
  }

  [Fact]
  public void GetHashCode_Should_Be_Stable_For_NonTransient()
  {
    var entity = new TestEntity(Guid.NewGuid());

    var hash1 = entity.GetHashCode();
    var hash2 = entity.GetHashCode();

    hash1.Should().Be(hash2);
  }

  [Fact]
  public void MarkCreated_Should_Set_Audit_Fields()
  {
    var entity = new TestEntity(Guid.NewGuid());

    entity.MarkCreated("tester");

    entity.CreatedBy.Should().Be("tester");
    entity.DateCreated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
  }

  [Fact]
  public void MarkUpdated_Should_Set_Modified_Fields()
  {
    var entity = new TestEntity(Guid.NewGuid());

    entity.MarkUpdated("modifier");

    entity.LastModifiedBy.Should().Be("modifier");
    entity.LastModifiedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
  }

  [Fact]
  public void MarkDeleted_Should_Set_Deletion_Fields()
  {
    var entity = new TestEntity(Guid.NewGuid());

    entity.MarkDeleted("deleter");

    entity.IsDeleted.Should().BeTrue();
    entity.DeletedBy.Should().Be("deleter");
    entity.DateDeleted.Should().NotBeNull();
  }
}
