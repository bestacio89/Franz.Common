using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Franz.Common.Business.Tests.Domain.ExtensionsTests;

internal static class TestAssemblyHelper
{
  public static Assembly LoadFakeApplicationAssembly(string assemblyName)
  {
    var name = new AssemblyName(assemblyName);

    return AssemblyBuilder
        .DefineDynamicAssembly(name, AssemblyBuilderAccess.Run)
        .DefineDynamicModule($"{assemblyName}.dll")
        .Assembly;
  }
}
