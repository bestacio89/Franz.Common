using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.SourceGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Franz.Common.Mediator.Tests.Generator;

public sealed class MediatorGeneratorDiagnosticTests
{
    [Fact]
    public void ReportsMissingHandlerForConcreteQueryDispatch()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using Franz.Common.Mediator.Dispatchers;
            using Franz.Common.Mediator.Messages;

            public sealed record MissingQuery(int Value) : IQuery<int>;

            public static class DispatchProbe
            {
                public static Task<int> Dispatch(IDispatcher dispatcher)
                    => dispatcher.SendAsync<int>(new MissingQuery(42));
            }
            """;

        var diagnostics = RunGenerator(source);

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "FRZMED001");
    }

    [Fact]
    public void DoesNotReportWhenConcreteQueryHandlerIsGenerated()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using Franz.Common.Mediator.Dispatchers;
            using Franz.Common.Mediator.Handlers;
            using Franz.Common.Mediator.Messages;

            public sealed record RegisteredQuery(int Value) : IQuery<int>;

            public sealed class RegisteredQueryHandler : IQueryHandler<RegisteredQuery, int>
            {
                public Task<int> Handle(RegisteredQuery query, CancellationToken cancellationToken)
                    => Task.FromResult(query.Value);
            }

            public static class DispatchProbe
            {
                public static Task<int> Dispatch(IDispatcher dispatcher)
                    => dispatcher.SendAsync<int>(new RegisteredQuery(42));
            }
            """;

        var diagnostics = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "FRZMED001");
    }

    private static Diagnostic[] RunGenerator(string source)
    {
        var platformAssemblies = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
        var references = platformAssemblies
            .Split(Path.PathSeparator)
            .Select(static path => MetadataReference.CreateFromFile(path))
            .Append(MetadataReference.CreateFromFile(typeof(IDispatcher).Assembly.Location));

        var compilation = CSharpCompilation.Create(
            assemblyName: "MediatorGeneratorDiagnosticProbe",
            syntaxTrees: new[] { CSharpSyntaxTree.ParseText(source) },
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new MediatorRegistrationGenerator().AsSourceGenerator());

        driver = driver.RunGenerators(compilation);
        return driver.GetRunResult().Diagnostics.ToArray();
    }
}