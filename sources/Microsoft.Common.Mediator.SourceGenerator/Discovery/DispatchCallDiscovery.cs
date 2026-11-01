using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Threading;

namespace Franz.Common.Mediator.SourceGenerator.Discovery;

internal sealed record DispatchCallInfo(
    string RequestType,
    string? ResponseType,
    string HandlerKind,
    string RequestName,
    Location Location);

internal static class DispatchCallDiscovery
{
    private const string DispatcherType = "Franz.Common.Mediator.Dispatchers.IDispatcher";
    private const string CommandRequestType = "Franz.Common.Mediator.Messages.ICommand<TResponse>";
    private const string VoidCommandRequestType = "Franz.Common.Mediator.Messages.ICommand";
    private const string QueryRequestType = "Franz.Common.Mediator.Messages.IQuery<TResponse>";

    public static bool IsCandidate(SyntaxNode node, CancellationToken cancellationToken)
        => node is InvocationExpressionSyntax invocation
            && invocation.Expression is MemberAccessExpressionSyntax member
            && member.Name.Identifier.ValueText is "SendAsync" or "PublishNotificationAsync" or "PublishEventAsync";

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

        var requestType = context.SemanticModel.GetTypeInfo(
            invocation.ArgumentList.Arguments[0].Expression,
            cancellationToken).Type as INamedTypeSymbol;

        if (requestType is null
            || requestType.TypeKind == TypeKind.Interface
            || requestType.IsAbstract
            || requestType.IsUnboundGenericType)
            return null;

        string? handlerKind;
        string? responseType = null;

        switch (method.Name)
        {
            case "SendAsync":
                if (method.Parameters[0].Type is not INamedTypeSymbol requestContract)
                    return null;

                handlerKind = requestContract.OriginalDefinition.ToDisplayString() switch
                {
                    CommandRequestType => "Command",
                    VoidCommandRequestType => "VoidCommand",
                    QueryRequestType => "Query",
                    _ => null
                };

                if (handlerKind is null)
                    return null;

                if (handlerKind is "Command" or "Query")
                {
                    if (requestContract.TypeArguments.Length != 1)
                        return null;

                    responseType = requestContract.TypeArguments[0]
                        .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                }
                break;

            case "PublishNotificationAsync":
                handlerKind = "Notification";
                break;

            case "PublishEventAsync":
                handlerKind = "Event";
                break;

            default:
                return null;
        }

        return new DispatchCallInfo(
            requestType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            responseType,
            handlerKind,
            requestType.Name,
            invocation.GetLocation());
    }
}