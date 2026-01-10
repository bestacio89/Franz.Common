using Franz.Common.Reflection;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Franz.Common.Messaging.Sagas.Tests.Fixtures;

using Franz.Common.Messaging.Sagas.Tests.Sagas;
using Franz.Common.Reflection;

public sealed class TestAssemblyAccessor : IAssemblyAccessor
{
  private readonly IAssembly _assembly;

  public TestAssemblyAccessor(IAssembly assembly)
  {
    _assembly = assembly;
  }

  public IAssembly GetEntryAssembly()
        => new AssemblyWrapper(typeof(TestSaga).Assembly);

}
