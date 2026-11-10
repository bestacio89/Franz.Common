using System.Diagnostics.CodeAnalysis;

namespace Franz.Common.Business.Domain.Factories;

public interface IAggregateFactory<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors |
                                DynamicallyAccessedMemberTypes.NonPublicConstructors)] TAggregate>
{
  TAggregate Create();
}