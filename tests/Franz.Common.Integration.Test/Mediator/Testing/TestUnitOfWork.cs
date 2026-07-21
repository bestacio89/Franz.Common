using Franz.Common.Mediator.Pipelines.Core;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Integration.Tests.Mediator.Testing
{
    internal class TestUnitOfWork : IUnitOfWork
    {
        public Task BeginAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
