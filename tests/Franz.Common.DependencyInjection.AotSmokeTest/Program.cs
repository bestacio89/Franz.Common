using Franz.Common.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddNoDuplicateSingleton<IAotProbe, AotProbe>();

using var provider = services.BuildServiceProvider();
var probe = provider.GetRequiredService<IAotProbe>();

return probe.GetValue() == "explicit-registration" ? 0 : 1;