using Franz.Common.Mediator.SourceGenerator.Discovery;
using Franz.Common.Mediator.SourceGenerator.Generation;
using Microsoft.CodeAnalysis;
using System.Linq;

namespace Franz.Common.Mediator.SourceGenerator;

[Generator(LanguageNames.CSharp)]
public sealed class MediatorRegistrationGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor MissingHandler = new(
        id: "FRZMED001",
        title: "Mediator handler registration is missing",
        messageFormat: "No generated {0} handler was found for request '{1}'. Register a concrete handler in this compilation or use a statically typed request.",
        category: "Franz.Mediator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Detects statically typed dispatcher calls whose request has no source-generated handler registration.");

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var handlerDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: HandlerDiscovery.IsCandidate,
                transform: HandlerDiscovery.Analyze)
            .Where(static handlers => handlers is not null)
            .SelectMany(static (handlers, _) => handlers!);

        var dispatchCalls = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: DispatchCallDiscovery.IsCandidate,
                transform: DispatchCallDiscovery.Analyze)
            .Where(static call => call is not null);

        context.RegisterSourceOutput(
            handlerDeclarations.Collect().Combine(dispatchCalls.Collect()),
            static (productionContext, input) =>
            {
                var handlers = input.Left;
                foreach (var call in input.Right)
                {
                    if (call is null)
                        continue;

                    var isRegistered = handlers.Any(handler =>
                        handler.HandlerKind == call.HandlerKind
                        && handler.RequestType == call.RequestType
                        && handler.ResponseType == call.ResponseType);

                    if (!isRegistered)
                    {
                        productionContext.ReportDiagnostic(Diagnostic.Create(
                            MissingHandler,
                            call.Location,
                            call.HandlerKind,
                            call.RequestName));
                    }
                }

                var source = RegistrationSourceEmitter.Generate(handlers);
                productionContext.AddSource("GeneratedMediatorRegistration.g.cs", source);
            });
    }
}