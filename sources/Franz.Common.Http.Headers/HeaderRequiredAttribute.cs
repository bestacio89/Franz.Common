using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.Http.Headers;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class HeaderRequiredAttribute : Attribute, IActionConstraintFactory
{
    public HeaderRequiredAttribute(Type type, string name)
      : base()
    {
        Type = type;
        Name = name;
    }

    public Type Type { get; }

    public string Name { get; }

    public bool IsReusable => true;

    public IActionConstraint CreateInstance(IServiceProvider services)
    {
        var result = services.GetRequiredService<HeaderRequiredActionConstraint>();

        result.HeaderType = Type;
        result.HeaderName = Name;

        return result;
    }
}
