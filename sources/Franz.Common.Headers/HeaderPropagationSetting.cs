namespace Franz.Common.Headers;

public class HeaderPropagationSetting : IHeaderPropagationSetting
{
    public HeaderPropagationSetting(string headerName)
    {
        HeaderName = headerName;
    }

    public string HeaderName { get; }
}
