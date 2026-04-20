using Franz.Common.Business.Domain;

namespace Franz.Common.EntityFramework.Tests.Repositories.Fakes;

public class DummyEntity : Entity<Guid>
{
  public string Name { get; private set; } = string.Empty;

  public TestEnum EnumProp { get; set; } = TestEnum.One;

  public DummyEntity()
  {
  }

  public void SetName(string name)
  {
    Name = name;
  }

  public void SetEnum(TestEnum value)
  {
    EnumProp = value;
  }
}