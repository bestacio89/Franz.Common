using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Threading;

namespace Franz.Common.Mediator.SourceGenerator.Discovery;

internal static class IntegrationEventDiscovery
{
  public static bool IsCandidate(SyntaxNode node, CancellationToken cancellationToken)
      => node is TypeDeclarationSyntax declaration && declaration.BaseList is not null;

  public static string? Analyze(GeneratorSyntaxContext context, CancellationToken cancellationToken)
  {
    if (context.SemanticModel.GetDeclaredSymbol(context.Node, cancellationToken) is not INamedTypeSymbol symbol)
      return null;

    if (symbol.IsAbstract || symbol.IsGenericType || symbol.TypeKind == TypeKind.Interface)
      return null;

    // Generated module-initializer code lives at namespace scope. It cannot legally
    // reference private or protected nested contracts, so only emit types accessible
    // from another type in the same assembly.
    for (var current = symbol; current is not null; current = current.ContainingType)
    {
      if (current.DeclaredAccessibility is Accessibility.Private
          or Accessibility.Protected
          or Accessibility.ProtectedAndInternal)
        return null;
    }

    var isIntegrationEvent = symbol.AllInterfaces.Any(static implementedInterface =>
        implementedInterface.ToDisplayString() == "Franz.Common.Mediator.IIntegrationEvent");

    return isIntegrationEvent
        ? symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
        : null;
  }
}
