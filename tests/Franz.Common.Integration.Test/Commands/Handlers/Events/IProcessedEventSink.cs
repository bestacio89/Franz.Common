using Franz.Common.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Integration.Tests.Commands.Handlers.Events;

public interface IProcessedEventSink : ISingletonDependency
{
  void Add(string name, Guid aggregateId);
  IReadOnlyCollection<(string name, Guid id)> All { get; }
}