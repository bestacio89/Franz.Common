using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Threading;

namespace Franz.Common.Mediator.SourceGenerator.Discovery;

internal sealed record DispatchCallInfo(
    string RequestType,
    string ResponseType,
    string HandlerKind,
    string RequestName,
    Location Location);

internal static class DispatchCallDiscovery
{
    private const string DispatcherType = "Franz.Common.Mediator.Dispatchers.IDispatcher";
    private const string CommandRequestType = "Franz.Common.Mediator.Messages.ICommand<TResponse>";
    private const string QueryRequestType = "Franz.Common.Mediator.Messages.IQuery<TResponse>";

    public static bool IsCandidate(SyntaxNode node, CancellationToken cancellationToken)
        => node is InvocationExpressionSyntax invocation
            && invocation.Expression is MemberAccessExpressionSyntax member
            && member.Name.Identifier.ValueText == "SendAsync";

    public static DispatchCallInfo? Analyze(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (context.SemanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol method
            || method.ContainingType.ToDisplayString() != DispatcherType
            || method.Parameters.Length == 0
            || invocation.ArgumentList.Arguments.Count == 0)
            return null;

        var parameterType = method.Parameters[0].Type;
        if (parameterType is not INamedTypeSymbol requestContract)
            return null;

        var contractName = requestContract.OriginalDefinition.ToDisplayString();
        var handlerKind = contractName switch
        {
            CommandRequestType => "Command",
            QueryRequestType => "Query",
            _ => null
        };

        if (handlerKind is null || requestContract.TypeArguments.Length != 1)
            return null;

        if (context.SemanticModel.GetTypeInfo(
                invocation.ArgumentList.Arguments[0].Expression,
                cancellationToken).Type is not INamedTypeSymbol requestType
            || requestType.IsAbstract
            || requestType.IsUnboundGenericType
            || requestType.IsGenericType)
            return null;

        return new DispatchCallInfo(
            requestType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            requestContract.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            handlerKind,
            requestType.Name,
            invocation.GetLocation());
    }
}