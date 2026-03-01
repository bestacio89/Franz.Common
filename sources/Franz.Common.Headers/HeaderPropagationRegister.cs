namespace Franz.Common.Headers;

public class HeaderPropagationRegister : IHeaderPropagationRegistrer
{

    public HeaderPropagationRegister(IEnumerable<IHeaderPropagationSetting>? headerPropagationSettings = null)

    {
        Headers = headerPropagationSettings ?? new List<IHeaderPropagationSetting>();
    }

    public IEnumerable<IHeaderPropagationSetting> Headers { get; }
}
