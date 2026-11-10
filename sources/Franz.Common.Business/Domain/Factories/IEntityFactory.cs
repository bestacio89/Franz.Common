using System.Diagnostics.CodeAnalysis;

namespace Franz.Common.Business.Domain.Factories;

public interface IEntityFactory<TKey,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors |
                                DynamicallyAccessedMemberTypes.NonPublicConstructors)] TEntity>
    where TEntity : Entity<TKey>
{
  TEntity Create();
}