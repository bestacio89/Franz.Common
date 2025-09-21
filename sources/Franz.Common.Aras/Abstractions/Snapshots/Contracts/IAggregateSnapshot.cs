using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Aras.Abstractions.Snapshots.Contracts;
public interface IAggregateSnapshot
{
  Guid AggregateId { get; }
  int Version { get; }
  DateTime Timestamp { get; }
}
