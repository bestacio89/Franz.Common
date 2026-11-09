#!/usr/bin/env bash
set -Eeuo pipefail

repository_root="$(git rev-parse --show-toplevel)"
temporary_root="$(mktemp -d)"
trap 'rm -rf "$temporary_root"' EXIT

feed="$temporary_root/feed"
consumer="$temporary_root/consumer"
mkdir -p "$feed" "$consumer/Protos"

find "$repository_root/sources" -path '*/bin/Release/*.nupkg' -exec cp -n {} "$feed/" \;

cat > "$consumer/Consumer.csproj" <<'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <PackageReference Include="Franz.Common.Grpc" Version="3.0.0" />
    <PackageReference Include="Franz.Common.Grpc.AspNetCore" Version="3.0.0" />
    <PackageReference Include="Google.Protobuf" Version="3.35.1" />
    <PackageReference Include="Grpc.AspNetCore" Version="2.84.0" />
    <PackageReference Include="Grpc.Net.ClientFactory" Version="2.84.0" />
    <PackageReference Include="Grpc.Tools" Version="2.84.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>
  <ItemGroup>
    <Protobuf Include="Protos/consumer.proto" GrpcServices="Client" />
  </ItemGroup>
</Project>
EOF

cat > "$consumer/Protos/consumer.proto" <<'EOF'
syntax = "proto3";

package franz.grpc.consumer.v1;
option csharp_namespace = "GrpcConsumer";

service Greeter {
  rpc Ping (PingRequest) returns (PingReply);
}

message PingRequest {
  string value = 1;
}

message PingReply {
  string value = 1;
}
EOF

cat > "$consumer/Program.cs" <<'EOF'
using Franz.Common.Grpc.Abstractions;
using Franz.Common.Grpc.AspNetCore.DependencyInjection;
using Franz.Common.Grpc.Client;
using Franz.Common.Grpc.DependencyInjection;
using GrpcConsumer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var configuration = new ConfigurationBuilder().Build();
var services = new ServiceCollection();

services
    .AddFranzGrpcDefaults()
    .AddFranzGrpcServer(configuration)
    .AddFranzGrpcClient(configuration)
    .AddFranzGrpcAspNetCore();

services.AddGrpcClient<Greeter.GreeterClient>(options =>
{
    options.Address = new Uri("https://localhost");
});
services.AddScoped<GreeterFranzClient>();

using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();

_ = scope.ServiceProvider.GetRequiredService<GreeterFranzClient>();

var context = GrpcCallContext.CreateClient("franz.grpc.consumer.v1.Greeter", "Ping");
if (context.Method != "/franz.grpc.consumer.v1.Greeter/Ping")
    return 1;

if (FranzGrpcDiagnostics.ActivitySourceName != "Franz.Common.Grpc")
    return 1;

return 0;

public sealed class GreeterFranzClient : FranzGrpcClient<Greeter.GreeterClient>
{
    public GreeterFranzClient(
        Greeter.GreeterClient client,
        IGrpcClientBehaviorProvider behaviorProvider)
        : base(client, behaviorProvider, "franz.grpc.consumer.v1.Greeter")
    {
    }

    public Task<PingReply> PingAsync(
        PingRequest request,
        CancellationToken cancellationToken = default)
        => UnaryAsync(
            request,
            "Ping",
            (client, message, options) => client.PingAsync(message, options).ResponseAsync,
            cancellationToken: cancellationToken);
}
EOF

dotnet restore "$consumer/Consumer.csproj" --source "$feed" --source https://api.nuget.org/v3/index.json
dotnet build "$consumer/Consumer.csproj" --configuration Release --no-restore
