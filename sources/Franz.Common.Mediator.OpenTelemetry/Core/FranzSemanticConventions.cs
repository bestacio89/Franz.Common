using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Mediator.OpenTelemetry.Core;

public static class FranzSemanticConventions
{
  public const string CorrelationId = "franz.correlation_id";
  public const string UserId = "franz.user_id";
  public const string TenantId = "franz.tenant_id";
  public const string Culture = "franz.culture";
  public const string Environment = "franz.environment";

  public const string MediatorType = "franz.mediator.type";
  public const string MediatorName = "franz.mediator.name";

  public const string EventName = "franz.event.name";
  public const string EventType = "franz.event.type";
}
