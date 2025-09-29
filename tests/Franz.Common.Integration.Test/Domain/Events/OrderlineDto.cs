using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.IntegrationTesting.Domain.Events;
public sealed record OrderLineDto(string Sku, int Quantity, decimal UnitPrice);
