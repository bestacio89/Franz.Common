using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Franz.Common.Business.Tests
{
  public abstract class TestBase
  {
    protected ILogger Logger { get; }

    protected TestBase() => Logger = NullLogger.Instance;
  }
}
