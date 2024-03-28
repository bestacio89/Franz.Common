using Franz.Common.Errors;

namespace Franz.Common.Reflection;

public static class AssemblyExtensions
{
    public static string ExtractParts(this IAssembly assembly, int number, string joinSeparator = ".")
    {
        if (assembly.FullName == null)
            throw new TechnicalException("Assembly's fullname is null");

        var parts = assembly.FullName.Split(".").Take(number);

        if (parts.Count() < number)
            throw new TechnicalException("Assembly's name is too short");

        var result = string.Join(joinSeparator, parts);

        return result;
    }
}
