namespace Franz.Common.Headers;

public class HeaderPropagationOptions
{
    public ICollection<string> Headers { get; } = new HashSet<string>();
}
