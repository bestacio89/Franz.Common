namespace Franz.Common.Messaging;

public static class HeaderNamer
{
    public static string GetEventClassName(Type type)
    {
        var result = $"{type.FullName}, {type.Assembly.GetName().Name}";

        return result;
    }
}
