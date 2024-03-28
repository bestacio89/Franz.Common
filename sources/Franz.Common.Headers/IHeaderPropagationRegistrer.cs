namespace Franz.Common.Headers;

public interface IHeaderPropagationRegistrer
{
    IEnumerable<IHeaderPropagationSetting> Headers { get; }
}
