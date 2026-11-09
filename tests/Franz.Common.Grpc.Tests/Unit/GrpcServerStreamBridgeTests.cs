using Franz.Common.Grpc.AspNetCore.Interceptors;
using Grpc.Core;

namespace Franz.Common.Grpc.Tests.Unit;

public sealed class GrpcServerStreamBridgeTests
{
    [Fact]
    public async Task WriteAsync_SnapshotsWriteOptionsForEachMessage()
    {
        var bridge = new GrpcServerStreamBridge<string>(CancellationToken.None);
        var firstOptions = new WriteOptions(WriteFlags.BufferHint);
        var secondOptions = new WriteOptions(WriteFlags.NoCompress);

        bridge.WriteOptions = firstOptions;
        await bridge.WriteAsync("first");

        bridge.WriteOptions = secondOptions;
        await bridge.WriteAsync("second");
        bridge.Complete();

        var messages = new List<(string Message, WriteOptions? WriteOptions)>();
        await foreach (var message in bridge.ReadAllAsync(CancellationToken.None))
            messages.Add(message);

        Assert.Collection(
            messages,
            first =>
            {
                Assert.Equal("first", first.Message);
                Assert.Same(firstOptions, first.WriteOptions);
            },
            second =>
            {
                Assert.Equal("second", second.Message);
                Assert.Same(secondOptions, second.WriteOptions);
            });
    }
}
