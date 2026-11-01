using Franz.Common.Mediator.SourceGenerator.Constants;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Threading;

namespace Franz.Common.Mediator.SourceGenerator.Discovery;

internal static class HandlerDiscovery
{
  private static readonly HashSet<string> TargetInterfaces = new()
  {
    MediatorKnownTypes.ICommandHandler,
    MediatorKnownTypes.IVoidCommandHandler,
    MediatorKnownTypes.IQueryHandler,
    MediatorKnownTypes.INotificationHandler,
    MediatorKnownTypes.IEventHandler,
    MediatorKnownTypes.IStreamQueryHandler,
    MediatorKnownTypes.IValidator
  };

  public static bool IsCandidate(SyntaxNode node, CancellationToken cancellationToken)
      => node is TypeDeclarationSyntax declaration && declaration.BaseList is not null;

  public static IEnumerable<HandlerInfo>? Analyze(
      GeneratorSyntaxContext context,
      CancellationToken cancellationToken)
  {
    if (context.SemanticModel.GetDeclaredSymbol(context.Node, cancellationToken) is not INamedTypeSymbol symbol)
      return null;

    if (symbol.IsAbstract || symbol.IsGenericType || symbol.TypeKind == TypeKind.Interface)
      return null;

    List<HandlerInfo>? discovered = null;

    foreach (var interfaceSymbol in symbol.AllInterfaces)
    {
      var originalDefinition = interfaceSymbol.OriginalDefinition.ToDisplayString();
      if (!TargetInterfaces.Contains(originalDefinition))
        continue;

      var requestType = interfaceSymbol.TypeArguments[0]
          .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
      var responseType = interfaceSymbol.TypeArguments.Length > 1
          ? interfaceSymbol.TypeArguments[1].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
          : null;
      var serviceType = interfaceSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
      var implementationType = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
      var kind = originalDefinition switch
      {
        MediatorKnownTypes.ICommandHandler => "Command",
        MediatorKnownTypes.IVoidCommandHandler => "VoidCommand",
        MediatorKnownTypes.IQueryHandler => "Query",
        MediatorKnownTypes.IEventHandler => "Event",
        MediatorKnownTypes.INotificationHandler => "Notification",
        MediatorKnownTypes.IStreamQueryHandler => "Stream",
        MediatorKnownTypes.IValidator => "Validator",
        _ => "Unknown"
      };

      discovered ??= new List<HandlerInfo>();
      discovered.Add(new HandlerInfo(serviceType, implementationType, kind, requestType, responseType));
    }

    return discovered;
  }
}
