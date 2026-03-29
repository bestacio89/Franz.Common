namespace Franz.Common.Mapping.Abstractions;

public interface IMapper<in TSource, out TDestination>
{
    TDestination Map(TSource source);
}
