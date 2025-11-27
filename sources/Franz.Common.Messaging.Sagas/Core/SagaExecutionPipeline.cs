#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.Sagas.Core;

/// <summary>
/// Execution pipeline for saga handlers.
/// Allows middleware to wrap saga step execution.
/// </summary>
public sealed class SagaExecutionPipeline
{
  private readonly List<Func<Func<Task>, Task>> _middlewares = new();

  public SagaExecutionPipeline Use(Func<Func<Task>, Task> middleware)
  {
    _middlewares.Add(middleware);
    return this;
  }

  public Task ExecuteAsync(Func<Task> final)
  {
    Task Handler(int index)
    {
      if (index == _middlewares.Count)
        return final();

      return _middlewares[index](() => Handler(index + 1));
    }

    return Handler(0);
  }
}
