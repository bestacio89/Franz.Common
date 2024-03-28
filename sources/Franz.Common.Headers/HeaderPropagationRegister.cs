namespace Franz.Common.Headers;

public class HeaderPropagationRegister : IHeaderPropagationRegistrer
{
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    public HeaderPropagationRegister(IEnumerable<IHeaderPropagationSetting>? headerPropagationSettings = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    {
        Headers = headerPropagationSettings ?? new List<IHeaderPropagationSetting>();
    }

    public IEnumerable<IHeaderPropagationSetting> Headers { get; }
}
