#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Franz.Common.Messaging.Manifest;

/// <summary>
/// Describes message destinations and independently discovered consumer subscriptions
/// for one compiled application or contract assembly.
/// </summary>
public sealed class MessagingManifest
{
  public MessagingManifest(
      IEnumerable<Type>? destinations = null,
      IEnumerable<Type>? subscriptions = null)
      : this(destinations, subscriptions, knownTypes: null)
  {
  }

  public MessagingManifest(
      IEnumerable<Type>? destinations,
      IEnumerable<Type>? subscriptions,
      IEnumerable<Type>? knownTypes)
  {
    Destinations = Normalize(destinations);
    Subscriptions = Normalize(subscriptions);
    KnownTypes = Normalize((knownTypes ?? Array.Empty<Type>())
        .Concat(Destinations)
        .Concat(Subscriptions));
  }

  public IReadOnlyList<Type> Destinations { get; }

  public IReadOnlyList<Type> Subscriptions { get; }

  /// <summary>All statically known request and message types emitted by the mediator generator.</summary>
  public IReadOnlyList<Type> KnownTypes { get; }

  private static IReadOnlyList<Type> Normalize(IEnumerable<Type>? types)
  {
    if (types is null)
      return Array.Empty<Type>();

    var materialized = types.ToArray();
    if (materialized.Any(static type => type is null))
      throw new ArgumentException("Manifest entries cannot contain null types.", nameof(types));

    return Array.AsReadOnly(materialized.Distinct().OrderBy(
        static type => type.FullName ?? type.Name,
        StringComparer.Ordinal).ToArray());
  }
}

/// <summary>
/// Stores generated or explicitly supplied manifests by their owning assembly.
/// </summary>
public static class MessagingManifestRegistry
{
  private static readonly ConcurrentDictionary<Assembly, MessagingManifest> Manifests = new();

  public static void Register(Assembly assembly, MessagingManifest manifest)
  {
    ArgumentNullException.ThrowIfNull(assembly);
    ArgumentNullException.ThrowIfNull(manifest);
    Manifests[assembly] = manifest;
  }

  public static MessagingManifest Get(Assembly assembly)
  {
    ArgumentNullException.ThrowIfNull(assembly);
    return Manifests.TryGetValue(assembly, out var manifest)
        ? manifest
        : new MessagingManifest();
  }

  public static IReadOnlyCollection<MessagingManifest> GetAll()
      => Manifests.Values.ToArray();
}
