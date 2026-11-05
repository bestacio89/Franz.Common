using FluentAssertions;
using Franz.Common.Messaging.Manifest;
using Xunit;
using System.Reflection;
using System.Reflection.Emit;

namespace Franz.Common.Messaging.Tests.Manifest;

public sealed class MessagingManifestTests
{
  [Fact]
  public void MessagingManifest_ShouldNormalizeAndSnapshotDestinationsAndSubscriptions()
  {
    var manifest = new MessagingManifest(
        destinations: new[] { typeof(Uri), typeof(string), typeof(Uri) },
        subscriptions: new[] { typeof(Guid), typeof(int), typeof(Guid) });

    manifest.Destinations.Select(static type => type.FullName)
        .Should().Equal(typeof(string).FullName, typeof(Uri).FullName);
    manifest.Subscriptions.Select(static type => type.FullName)
        .Should().Equal(typeof(Guid).FullName, typeof(int).FullName);
    manifest.KnownTypes.Should().Contain(new[] { typeof(Uri), typeof(string), typeof(Guid), typeof(int) });
  }

  [Fact]
  public void MessagingManifestRegistry_ShouldReturnEmptyManifestForUnregisteredAssembly()
  {
    var assembly = AssemblyBuilder.DefineDynamicAssembly(
        new AssemblyName($"Unregistered-{Guid.NewGuid():N}"), AssemblyBuilderAccess.Run);
    var manifest = MessagingManifestRegistry.Get(assembly);

    manifest.Destinations.Should().NotBeNull().And.BeEmpty();
    manifest.Subscriptions.Should().NotBeNull().And.BeEmpty();
    manifest.KnownTypes.Should().NotBeNull().And.BeEmpty();
  }

  [Fact]
  public void MessagingManifestRegistry_ShouldRegisterManifestByAssembly()
  {
    var assembly = AssemblyBuilder.DefineDynamicAssembly(
        new AssemblyName($"Registered-{Guid.NewGuid():N}"), AssemblyBuilderAccess.Run);
    var manifest = new MessagingManifest(
        destinations: new[] { typeof(string) },
        subscriptions: new[] { typeof(Guid) });

    MessagingManifestRegistry.Register(assembly, manifest);

    MessagingManifestRegistry.Get(assembly).Should().BeSameAs(manifest);
  }
}
