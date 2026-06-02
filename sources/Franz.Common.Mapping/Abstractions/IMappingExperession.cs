namespace Franz.Common.Mapping.Abstractions;

public interface IMappingExpression
{
  Type SourceType { get; }
  Type DestinationType { get; }

  IReadOnlyDictionary<string, string> MemberBindings { get; }
  IReadOnlyCollection<string> IgnoredMembers { get; }

  bool IsStrict { get; }

  bool HasConstructor { get; }
  Delegate? Constructor { get; }
  Delegate? ReverseConstructor { get; }
}