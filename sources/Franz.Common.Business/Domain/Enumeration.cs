namespace Franz.Common.Business.Domain;

public abstract class Enumeration : Enumeration<int>
{
  protected Enumeration(int id, string name) : base(id, name) { }
}