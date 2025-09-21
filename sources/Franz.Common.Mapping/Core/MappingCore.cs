using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Mapping.Core;
public class MappingExpression<TSource, TDestination>
{
  private readonly Dictionary<string, string> _memberMap = new();
  private readonly HashSet<string> _ignored = new();

  public MappingExpression<TSource, TDestination> ForMember(
      string destinationMember,
      string sourceMember)
  {
    _memberMap[destinationMember] = sourceMember;
    return this;
  }

  public MappingExpression<TSource, TDestination> Ignore(string destinationMember)
  {
    _ignored.Add(destinationMember);
    return this;
  }

  internal Dictionary<string, string> MemberMap => _memberMap;
  internal HashSet<string> Ignored => _ignored;
}