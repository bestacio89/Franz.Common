using Franz.Common.Mapping.Abstractions;

namespace Franz.Common.AotSmokeTest;

public static class Program
{
    public static int Main()
    {
        var source = new AotSource("Franz", 10);
        var destination = new AotMapper().Map(source);

        return destination.Name == source.Name && destination.Version == source.Version
            ? 0
            : 1;
    }
}

public sealed record AotSource(string Name, int Version);

public sealed class AotDestination
{
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; }
}

public partial class AotMapper : IMapper<AotSource, AotDestination>
{
    public partial AotDestination Map(AotSource source);
}
