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
        MediatorKnownTypes.IQueryHandler,
        MediatorKnownTypes.INotificationHandler,
        MediatorKnownTypes.IEventHandler,
        MediatorKnownTypes.IStreamQueryHandler,
        MediatorKnownTypes.IValidator
    };

  public static bool IsCandidate(SyntaxNode node, CancellationToken cancellationToken)
  {
    return node is ClassDeclarationSyntax classDecl && classDecl.BaseList is not null;
  }

  public static IEnumerable<HandlerInfo>? Analyze(
      GeneratorSyntaxContext context,
      CancellationToken cancellationToken)
  {
    var classDeclaration = (ClassDeclarationSyntax)context.Node;

    if (context.SemanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken) is not INamedTypeSymbol symbol)
      return null;

    if (symbol.IsAbstract || symbol.IsGenericType)
      return null;

    List<HandlerInfo>? discovered = null;

    foreach (var interfaceSymbol in symbol.AllInterfaces)
    {
      // Get original unbound generic definition (e.g. Franz.Common.Mediator.Handlers.ICommandHandler`2)
      var originalDefinition = interfaceSymbol.OriginalDefinition.ToDisplayString();

      if (!TargetInterfaces.Contains(originalDefinition))
        continue;

      discovered ??= new List<HandlerInfo>();

      var serviceType = interfaceSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
      var implementationType = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

      discovered.Add(new HandlerInfo(serviceType, implementationType));
    }

    return discovered;
  }
}