using System.Reflection;

namespace Franz.Common.Reflection;

public interface IAssembly
{
  string? Name { get; }
  string? FullName { get; }

  /// <summary>
  /// Underlying reflection assembly.
  /// Never null once constructed.
  /// </summary>
  Assembly Assembly { get; }
}
