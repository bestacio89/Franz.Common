using Franz.Common.Testing.Properties;
using Moq;
using System.Globalization;

namespace Franz.Common.Testing;

public class UnitTest : IDisposable
{
  protected MockRepository MockRepository { get; }

  public UnitTest()
    : this(Resources.DefaultNameCulture)
  {
  }

  public UnitTest(string cultureName)
  {
    MockRepository = new MockRepository(MockBehavior.Strict);
    Thread.CurrentThread.CurrentCulture = new CultureInfo(cultureName);
    Thread.CurrentThread.CurrentUICulture = new CultureInfo(cultureName);
  }

  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  ~UnitTest()
  {
    Dispose(false);
  }

  protected virtual void Dispose(bool disposing)
  {
    if (disposing)
      MockRepository.VerifyAll();
  }
}
