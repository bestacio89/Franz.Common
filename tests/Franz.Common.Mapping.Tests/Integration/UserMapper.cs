using Franz.Common.Mapping.Abstractions;
using Franz.Common.Mapping.Tests.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Mapping.Tests.Integration;

public class UserMapper : IMapper<User, UserDto>
{
  private static int _instantiations;
  public static int Instantiations => _instantiations;

  public UserMapper()
      => Interlocked.Increment(ref _instantiations);

  public UserDto Map(User source)
      => new() { Name = source.Name };

  public ValueTask<UserDto> MapAsync(User source, CancellationToken cancellationToken = default)
      => new(new UserDto { Name = source.Name });

  public static void Reset() => _instantiations = 0;
}

