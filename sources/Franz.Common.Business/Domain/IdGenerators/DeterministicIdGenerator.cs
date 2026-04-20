using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Franz.Common.Business.Domain.IdGenerators;

public sealed class DeterministicIdGenerator : IIdGenerator<Guid>
{
  public Guid Create(string input)
  {
    var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
    return new Guid(hash[..16]);
  }

  Guid IIdGenerator<Guid>.Create()
      => throw new NotSupportedException("Use Create(string input)");
}