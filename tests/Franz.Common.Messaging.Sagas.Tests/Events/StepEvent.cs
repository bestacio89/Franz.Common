using Franz.Common.Mediator;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Messaging.Sagas.Tests.Events;

public sealed record StepEvent(string Id) : IIntegrationEvent;