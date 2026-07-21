using Franz.Common.Mediator.SourceGenerator.Discovery;
using Franz.Common.Mediator.SourceGenerator.Generation;
using Microsoft.CodeAnalysis;
using System.Linq;

namespace Franz.Common.Mediator.SourceGenerator;

[Generator(LanguageNames.CSharp)]
public sealed class MediatorRegistrationGenerator : IIncrementalGenerator
{
  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    var handlerDeclarations = context.SyntaxProvider
        .CreateSyntaxProvider(
            predicate: HandlerDiscovery.IsCandidate,
            transform: HandlerDiscovery.Analyze)
        .Where(static x => x is not null);

    var combinedHandlers = handlerDeclarations
        .SelectMany(static (handlers, _) => handlers!);

    context.RegisterSourceOutput(
        combinedHandlers.Collect(),
        static (spc, handlers) =>
        {
          var source = RegistrationSourceEmitter.Generate(handlers);
          spc.AddSource("GeneratedMediatorRegistration.g.cs", source);
        });
  }
}