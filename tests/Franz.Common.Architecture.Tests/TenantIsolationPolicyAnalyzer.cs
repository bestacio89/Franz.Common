using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Franz.Common.Architecture.Tests;

/// <summary>
/// Source-level guardrails for tenant query isolation and SQL escape hatches.
/// The analyzer deliberately operates on syntax rather than executing provider code.
/// </summary>
internal static class TenantIsolationPolicyAnalyzer
{
    private static readonly HashSet<string> RawSqlMethods = new(StringComparer.Ordinal)
    {
        "FromSql",
        "FromSqlRaw",
        "FromSqlInterpolated",
        "ExecuteSql",
        "ExecuteSqlAsync",
        "ExecuteSqlRaw",
        "ExecuteSqlRawAsync",
        "ExecuteSqlInterpolated",
        "ExecuteSqlInterpolatedAsync",
        "SqlQuery",
        "SqlQueryAsync",
        "SqlQueryRaw",
        "SqlQueryRawAsync"
    };

    internal static IReadOnlySet<string> Analyze(string source, string path)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var findings = new HashSet<string>(StringComparer.Ordinal);
        var root = CSharpSyntaxTree.ParseText(source, path: path).GetRoot();

        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var methodName = GetInvokedMethodName(invocation.Expression);

            if (methodName == "IgnoreQueryFilters")
                findings.Add("QUERY_FILTER_BYPASS");

            if (RawSqlMethods.Contains(methodName))
                findings.Add("RAW_SQL");
        }

        foreach (var classDeclaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>()
            .Where(type => type.Identifier.ValueText == "DbContextBase"))
        {
            var modelBuilderMethod = classDeclaration.Members
                .OfType<MethodDeclarationSyntax>()
                .SingleOrDefault(method => method.Identifier.ValueText == "OnModelCreating");

            if (modelBuilderMethod is null || !HasTenantQueryFilter(modelBuilderMethod))
                findings.Add("TENANT_QUERY_FILTER_MISSING");
        }

        return findings;
    }

    private static bool HasTenantQueryFilter(MethodDeclarationSyntax method)
    {
        var invocations = method.DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();

        var identifiesTenantEntities = invocations.Any(invocation =>
            GetInvokedMethodName(invocation.Expression) == "IsAssignableFrom"
            && invocation.ToString().Contains("ITenantEntity", StringComparison.Ordinal));

        var appliesQueryFilter = invocations.Any(invocation =>
            GetInvokedMethodName(invocation.Expression) == "HasQueryFilter");

        var scopesFilterToCurrentTenant = method.DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .Any(identifier => identifier.Identifier.ValueText == "CurrentTenantId")
            && method.DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Any(identifier => identifier.Identifier.ValueText == "TenantId");

        return identifiesTenantEntities && appliesQueryFilter && scopesFilterToCurrentTenant;
    }

    private static string GetInvokedMethodName(ExpressionSyntax expression)
    {
        var name = expression switch
        {
            MemberAccessExpressionSyntax member => member.Name,
            GenericNameSyntax generic => generic,
            _ => expression
        };

        return name switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            GenericNameSyntax generic => generic.Identifier.ValueText,
            _ => name.ToString()
        };
    }
}
