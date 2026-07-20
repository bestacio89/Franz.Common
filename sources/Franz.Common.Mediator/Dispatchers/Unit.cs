#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Franz.Common.Mediator.Dispatchers;

/// <summary>
/// Represents a zero-byte void type for messaging contracts and pipeline continuations.
/// Highly optimized for zero allocations, struct equality, and JIT inlining.
/// </summary>
public readonly struct Unit : IEquatable<Unit>, IComparable<Unit>, IComparable
{
  /// <summary>
  /// Gets the single default instance of the <see cref="Unit"/> struct.
  /// Uses default struct value initialization to avoid heap allocation.
  /// </summary>
  public static Unit Value => default;

  /// <summary>
  /// Gets a completed <see cref="System.Threading.Tasks.Task{Unit}"/> to prevent task allocation on synchronous completions.
  /// </summary>
  public static System.Threading.Tasks.Task<Unit> Task { get; } = System.Threading.Tasks.Task.FromResult(default(Unit));

  /// <inheritdoc />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public bool Equals(Unit other) => true;

  /// <inheritdoc />
  public override bool Equals([NotNullWhen(true)] object? obj) => obj is Unit;

  /// <inheritdoc />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public override int GetHashCode() => 0;

  /// <inheritdoc />
  public override string ToString() => "()";

  /// <inheritdoc />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public int CompareTo(Unit other) => 0;

  /// <inheritdoc />
  public int CompareTo(object? obj)
  {

    if (obj is null) return 1;
    if (obj is Unit) return 0;
    throw new ArgumentException($"Object must be of type {nameof(Unit)}.");
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static bool operator ==(Unit left, Unit right) => true;

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static bool operator !=(Unit left, Unit right) => false;

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static bool operator <(Unit left, Unit right) => false;

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static bool operator <=(Unit left, Unit right) => true;

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static bool operator >(Unit left, Unit right) => false;

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static bool operator >=(Unit left, Unit right) => true;
}