using Franz.Common.Mediator.Options;
using Franz.Common.Mediator.Pipelines.Core;
using Franz.Common.Mediator.Pipelines.Transaction;

namespace Franz.Common.Mediator.Tests;

public sealed class TransactionPipelineTests
{
    [Fact]
    public async Task Handle_WhenRequestCancellationOccurs_RollsBackWithAnUncancelledToken()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var unitOfWork = new RecordingUnitOfWork();
        var pipeline = new TransactionPipeline<TestRequest, string>(
            unitOfWork,
            new TransactionOptions());

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            pipeline.Handle(
                new TestRequest(),
                () => Task.FromException<string>(new OperationCanceledException(cancellation.Token)),
                cancellation.Token));

        Assert.True(unitOfWork.BeginCalled);
        Assert.True(unitOfWork.RollbackCalled);
        Assert.False(unitOfWork.RollbackToken.IsCancellationRequested);
    }

    private sealed record TestRequest;

    private sealed class RecordingUnitOfWork : IUnitOfWork
    {
        public bool BeginCalled { get; private set; }
        public bool RollbackCalled { get; private set; }
        public CancellationToken RollbackToken { get; private set; }

        public Task BeginAsync(CancellationToken cancellationToken = default)
        {
            BeginCalled = true;
            return Task.CompletedTask;
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            RollbackCalled = true;
            RollbackToken = cancellationToken;
            return Task.CompletedTask;
        }
    }
}
