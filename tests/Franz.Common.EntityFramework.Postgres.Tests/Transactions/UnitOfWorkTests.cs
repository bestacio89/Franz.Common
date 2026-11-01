using Franz.Common.EntityFramework;
using Franz.Common.Mediator.Pipelines.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.EntityFramework.Postgres.Tests.Transactions;

public sealed class UnitOfWorkTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Commit_PersistsChangesVisibleFromANewContext()
    {
        var marker = $"uow-commit-{Guid.NewGuid():N}";

        await using (var provider = (ServiceProvider)fixture.BuildServiceProvider())
        await using (var scope = provider.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            await context.Database.EnsureCreatedAsync();

            await using var unitOfWork = new EfUnitOfWork<TestDbContext>(context);
            await unitOfWork.BeginAsync();

            context.TestEntities.Add(new TestEntity { Name = marker });
            await ((IUnitOfWork)unitOfWork).CommitAsync();
        }

        await using var verifyProvider = (ServiceProvider)fixture.BuildServiceProvider();
        await using var verifyScope = verifyProvider.CreateAsyncScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<TestDbContext>();

        Assert.True(await verifyContext.TestEntities.AnyAsync(entity => entity.Name == marker));
    }

    [Fact]
    public async Task Rollback_RemovesChangesFromPersistedState()
    {
        var marker = $"uow-rollback-{Guid.NewGuid():N}";

        await using (var provider = (ServiceProvider)fixture.BuildServiceProvider())
        await using (var scope = provider.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            await context.Database.EnsureCreatedAsync();

            await using var unitOfWork = new EfUnitOfWork<TestDbContext>(context);
            await unitOfWork.BeginAsync();

            context.TestEntities.Add(new TestEntity { Name = marker });
            await context.SaveChangesAsync();
            await unitOfWork.RollbackAsync();
        }

        await using var verifyProvider = (ServiceProvider)fixture.BuildServiceProvider();
        await using var verifyScope = verifyProvider.CreateAsyncScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<TestDbContext>();

        Assert.False(await verifyContext.TestEntities.IgnoreQueryFilters()
            .AnyAsync(entity => entity.Name == marker));
    }

    [Fact]
    public async Task Begin_RejectsASecondActiveTransaction()
    {
        await using var provider = (ServiceProvider)fixture.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        await context.Database.EnsureCreatedAsync();

        await using var unitOfWork = new EfUnitOfWork<TestDbContext>(context);
        await unitOfWork.BeginAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => unitOfWork.BeginAsync());
        await unitOfWork.RollbackAsync();
    }

    [Fact]
    public async Task Dispose_RollsBackAnUncompletedTransaction()
    {
        var marker = $"uow-dispose-{Guid.NewGuid():N}";

        await using (var provider = (ServiceProvider)fixture.BuildServiceProvider())
        await using (var scope = provider.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            await context.Database.EnsureCreatedAsync();

            var unitOfWork = new EfUnitOfWork<TestDbContext>(context);
            await unitOfWork.BeginAsync();
            context.TestEntities.Add(new TestEntity { Name = marker });
            await context.SaveChangesAsync();

            await unitOfWork.DisposeAsync();
        }

        await using var verifyProvider = (ServiceProvider)fixture.BuildServiceProvider();
        await using var verifyScope = verifyProvider.CreateAsyncScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<TestDbContext>();

        Assert.False(await verifyContext.TestEntities.IgnoreQueryFilters()
            .AnyAsync(entity => entity.Name == marker));
    }
}
