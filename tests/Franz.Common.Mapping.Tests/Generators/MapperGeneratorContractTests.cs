using Franz.Common.Mapping.Abstractions;
using Xunit;

namespace Franz.Common.Mapping.Tests.Generators;

public sealed record GeneratedMapperSource(string Name, int Age);

public sealed class GeneratedMapperDestination
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
}

public partial class GeneratedMapper : IMapper<GeneratedMapperSource, GeneratedMapperDestination>
{
    public partial GeneratedMapperDestination Map(GeneratedMapperSource source);
}

public sealed class MapperGeneratorContractTests
{
    [Fact]
    public void Generated_mapper_maps_matching_public_properties()
    {
        var mapper = new GeneratedMapper();

        var result = mapper.Map(new GeneratedMapperSource("Franz", 10));

        Assert.Equal("Franz", result.Name);
        Assert.Equal(10, result.Age);
    }
}
