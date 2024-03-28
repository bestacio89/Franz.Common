using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.Extensions.Primitives;

using Newtonsoft.Json;

namespace Franz.Common.Http.Headers;

public class HeaderRequiredActionConstraint : IActionConstraint
{
  public int Order => int.MaxValue;

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public Type? HeaderType { get; set; }
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

  public string HeaderName { get; set; } = null!;

  public virtual bool Accept(ActionConstraintContext context)
  {
    var result = false;

    if (context.RouteContext.HttpContext.Request.Headers.TryGetValue(HeaderName, out var value) && !value.IsNullOrEmpty())
      result = CheckHeaderValue(value);

    return result;
  }

  public bool CheckHeaderValue(StringValues value)
  {
    var result = false;

    var stringValue = value.ToString();
    if (HeaderType != null && !string.IsNullOrEmpty(stringValue))
    {
      result = HeaderType.Equals(typeof(string))
|| (HeaderType.Equals(typeof(Guid))
        ? Guid.TryParse(stringValue, out _) || JsonConvert.DeserializeObject(stringValue, HeaderType) != null
        : JsonConvert.DeserializeObject(stringValue, HeaderType) != null);
    }

    return result;
  }
}
