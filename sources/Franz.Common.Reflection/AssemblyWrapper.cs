#nullable enable
using System;
using System.Reflection;

namespace Franz.Common.Reflection;

public sealed class AssemblyWrapper : IAssembly
{
  public Assembly Assembly { get; }

  public string? Name => Assembly.GetName().Name;
  public string? FullName => Assembly.GetName().FullName;

  public AssemblyWrapper(Assembly assembly)
  {
    Assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
  }
}
