using System.Reflection;
using System.Text.Json;
using System.Xml.Linq;
using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Franz.Common.Architecture.Tests;

public sealed class FrameworkArchitectureTests
{
  private const string AdapterNamespacePattern =
    @"^Franz\.Common\.(EntityFramework(\..*)?|MongoDB(\..*)?|AzureCosmosDB(\..*)?|Http(\..*)?|Grpc(\..*)?|Messaging\..+|SSO(\..*)?)$";

  private static readonly Lazy<string> RepositoryRoot = new(FindRepositoryRoot);
  private static readonly Lazy<IReadOnlyList<Assembly>> RuntimeAssemblies = new(LoadRuntimeAssemblies);
  private static readonly Lazy<Architecture> ArchitectureModel = new(
    () => new ArchLoader().LoadAssemblies(RuntimeAssemblies.Value.ToArray()).Build());

  [Fact]
  public void Core_packages_must_not_depend_on_infrastructure_adapters()
  {
    var corePackages = new[]
    {
      "Franz.Common",
      "Franz.Common.Annotations",
      "Franz.Common.Business",
      "Franz.Common.DependencyInjection",
      "Franz.Common.Errors",
      "Franz.Common.Headers",
      "Franz.Common.Mediator",
      "Franz.Common.Messaging",
      "Franz.Common.MultiTenancy",
      "Franz.Common.Serialization"
    };

    foreach (var packageName in corePackages)
    {
      var assembly = RuntimeAssemblies.Value.SingleOrDefault(
        candidate => string.Equals(candidate.GetName().Name, packageName, StringComparison.Ordinal));

      Assert.True(assembly is not null, $"Core package assembly '{packageName}' was not loaded.");

      ArchRuleDefinition
        .Classes()
        .That()
        .ResideInAssembly(assembly!)
        .Should()
        .NotDependOnAnyTypesThat()
        .ResideInNamespaceMatching(AdapterNamespacePattern)
        .Because($"Core package '{packageName}' must depend on abstractions, not infrastructure adapters.")
        .Check(ArchitectureModel.Value);
    }
  }

  [Fact]
  public void Runtime_project_dependency_graph_must_be_acyclic()
  {
    var projects = LoadRuntimeProjectGraph();
    var cycles = FindCycles(projects);

    Assert.True(
      cycles.Count == 0,
      "Runtime project dependency cycles were found:" + Environment.NewLine +
      string.Join(Environment.NewLine, cycles));
  }

  [Fact]
  public void Runtime_projects_must_not_reference_test_projects_or_test_helpers()
  {
    var sourceRoot = Path.Combine(RepositoryRoot.Value, "sources");
    var testProjectNames = Directory
      .EnumerateFiles(sourceRoot, "*.Tests.csproj", SearchOption.AllDirectories)
      .Select(Path.GetFileNameWithoutExtension)
      .Append("Franz.Common.Testing")
      .ToHashSet(StringComparer.OrdinalIgnoreCase);

    var violations = new List<string>();

    foreach (var projectPath in Directory.EnumerateFiles(sourceRoot, "*.csproj", SearchOption.AllDirectories))
    {
      var projectName = Path.GetFileNameWithoutExtension(projectPath);
      if (projectName.EndsWith(".Tests", StringComparison.OrdinalIgnoreCase)
        || string.Equals(projectName, "Franz.Common.Testing", StringComparison.OrdinalIgnoreCase))
        continue;

      var document = XDocument.Load(projectPath);
      foreach (var reference in document
        .Descendants()
        .Where(element => element.Name.LocalName == "ProjectReference")
        .Select(element => (string?)element.Attribute("Include"))
        .Where(include => !string.IsNullOrWhiteSpace(include)))
      {
        var referencedName = Path.GetFileNameWithoutExtension(reference);
        if (testProjectNames.Contains(referencedName))
          violations.Add($"{projectName} -> {referencedName}");
      }
    }

    Assert.Empty(violations);
  }

  [Fact]
  public void Source_generator_must_target_netstandard20()
  {
    var projectPath = Path.Combine(
      RepositoryRoot.Value,
      "sources",
      "Microsoft.Common.Mediator.SourceGenerator",
      "Franz.Common.Mediator.SourceGenerator.csproj");

    Assert.True(File.Exists(projectPath), $"Source generator project was not found at '{projectPath}'.");

    var document = XDocument.Load(projectPath);
    var targetFramework = document
      .Descendants()
      .FirstOrDefault(element => element.Name.LocalName == "TargetFramework")
      ?.Value
      .Trim();

    Assert.Equal("netstandard2.0", targetFramework);
  }

  [Fact]
  public void Runtime_source_policy_violations_must_be_explicitly_allowlisted()
  {
    var findings = FindRuntimeSourcePolicyViolations();
    var allowlistPath = Path.Combine(
      AppContext.BaseDirectory,
      "architecture-policy-allowlist.json");

    Assert.True(File.Exists(allowlistPath), "The architecture policy allowlist was not copied to the test output.");

    var allowlist = JsonSerializer.Deserialize<List<PolicyException>>(
      File.ReadAllText(allowlistPath),
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

    var allowed = allowlist.ToDictionary(
      exception => $"{exception.Rule}|{NormalizePath(exception.Path)}",
      exception => exception.Reason,
      StringComparer.Ordinal);

    var unexpected = findings
      .Where(finding => !allowed.ContainsKey($"{finding.Rule}|{finding.Path}"))
      .Select(finding => $"{finding.Rule}: {finding.Path}")
      .ToArray();

    var stale = allowed.Keys
      .Except(findings.Select(finding => $"{finding.Rule}|{finding.Path}"), StringComparer.Ordinal)
      .ToArray();

    var missingReasons = allowlist
      .Where(exception => string.IsNullOrWhiteSpace(exception.Reason))
      .Select(exception => $"{exception.Rule}: {exception.Path}")
      .ToArray();

    Assert.True(
      unexpected.Length == 0,
      "New architecture policy violations require remediation or a reviewed allowlist entry:" +
      Environment.NewLine + string.Join(Environment.NewLine, unexpected));

    Assert.True(
      stale.Length == 0,
      "Remove resolved entries from the shrinking architecture policy allowlist:" +
      Environment.NewLine + string.Join(Environment.NewLine, stale));

    Assert.True(
      missingReasons.Length == 0,
      "Every allowlist entry must explain why it is temporarily accepted:" +
      Environment.NewLine + string.Join(Environment.NewLine, missingReasons));
  }

  private static IReadOnlyList<Assembly> LoadRuntimeAssemblies()
  {
    var assemblies = Directory
      .EnumerateFiles(AppContext.BaseDirectory, "*.dll", SearchOption.TopDirectoryOnly)
      .Where(path =>
      {
        var name = Path.GetFileNameWithoutExtension(path);
        var isFrameworkAssembly = string.Equals(name, "Franz.Common", StringComparison.Ordinal)
          || name.StartsWith("Franz.Common.", StringComparison.Ordinal);

        return isFrameworkAssembly
          && !name.EndsWith(".Tests", StringComparison.OrdinalIgnoreCase)
          && !name.EndsWith(".Testing", StringComparison.OrdinalIgnoreCase)
          && !name.Contains(".SourceGenerator", StringComparison.OrdinalIgnoreCase);
      })
      .Select(Assembly.LoadFrom)
      .GroupBy(assembly => assembly.GetName().Name, StringComparer.Ordinal)
      .Select(group => group.First())
      .ToArray();

    Assert.NotEmpty(assemblies);
    return assemblies;
  }

  private static Dictionary<string, HashSet<string>> LoadRuntimeProjectGraph()
  {
    var sourceRoot = Path.Combine(RepositoryRoot.Value, "sources");
    var projectFiles = Directory
      .EnumerateFiles(sourceRoot, "*.csproj", SearchOption.AllDirectories)
      .Where(path =>
      {
        var name = Path.GetFileNameWithoutExtension(path);
        return !name.EndsWith(".Tests", StringComparison.OrdinalIgnoreCase)
          && !string.Equals(name, "Franz.Common.Testing", StringComparison.OrdinalIgnoreCase);
      })
      .ToArray();

    var knownProjects = projectFiles.ToDictionary(
      path => Path.GetFullPath(path),
      path => Path.GetFileNameWithoutExtension(path),
      StringComparer.OrdinalIgnoreCase);

    var graph = knownProjects.Values.ToDictionary(
      name => name,
      _ => new HashSet<string>(StringComparer.Ordinal),
      StringComparer.Ordinal);

    foreach (var projectPath in projectFiles)
    {
      var projectName = Path.GetFileNameWithoutExtension(projectPath);
      var document = XDocument.Load(projectPath);

      foreach (var reference in document
        .Descendants()
        .Where(element => element.Name.LocalName == "ProjectReference")
        .Select(element => (string?)element.Attribute("Include"))
        .Where(include => !string.IsNullOrWhiteSpace(include)))
      {
        var referencedPath = Path.GetFullPath(Path.Combine(
          Path.GetDirectoryName(projectPath)!,
          reference!.Replace('\\', Path.DirectorySeparatorChar)));

        if (knownProjects.TryGetValue(referencedPath, out var referencedName))
          graph[projectName].Add(referencedName);
      }
    }

    return graph;
  }

  private static IReadOnlyList<string> FindCycles(
    IReadOnlyDictionary<string, HashSet<string>> graph)
  {
    var state = new Dictionary<string, int>(StringComparer.Ordinal);
    var stack = new List<string>();
    var cycles = new HashSet<string>(StringComparer.Ordinal);

    void Visit(string node)
    {
      state[node] = 1;
      stack.Add(node);

      foreach (var dependency in graph[node])
      {
        if (!graph.ContainsKey(dependency))
          continue;

        if (!state.TryGetValue(dependency, out var dependencyState))
        {
          Visit(dependency);
          continue;
        }

        if (dependencyState != 1)
          continue;

        var start = stack.IndexOf(dependency);
        if (start >= 0)
        {
          var cycle = stack.Skip(start).Append(dependency);
          cycles.Add(string.Join(" -> ", cycle));
        }
      }

      stack.RemoveAt(stack.Count - 1);
      state[node] = 2;
    }

    foreach (var node in graph.Keys)
    {
      if (!state.ContainsKey(node))
        Visit(node);
    }

    return cycles.OrderBy(cycle => cycle, StringComparer.Ordinal).ToArray();
  }

  private static IReadOnlyList<PolicyFinding> FindRuntimeSourcePolicyViolations()
  {
    var sourceRoot = Path.Combine(RepositoryRoot.Value, "sources");
    var findings = new HashSet<PolicyFinding>();

    foreach (var projectFile in Directory.EnumerateFiles(sourceRoot, "*.Tests.csproj", SearchOption.AllDirectories))
    {
      findings.Add(new PolicyFinding(
        "TEST_PROJECT_IN_SOURCES",
        NormalizePath(Path.GetRelativePath(RepositoryRoot.Value, projectFile))));
    }

    foreach (var file in Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories))
    {
      var relativePath = NormalizePath(Path.GetRelativePath(RepositoryRoot.Value, file));

      if (relativePath.StartsWith("sources/Franz.Common.Testing/", StringComparison.Ordinal)
        || relativePath.StartsWith("sources/Microsoft.Common.Mediator.SourceGenerator/", StringComparison.Ordinal)
        || relativePath.Split('/').Any(segment =>
          segment is "bin" or "obj" || segment.EndsWith(".Tests", StringComparison.OrdinalIgnoreCase)))
        continue;

      if (relativePath.Split('/').Contains("Testing", StringComparer.Ordinal))
        findings.Add(new PolicyFinding("TEST_HELPER_IN_RUNTIME", relativePath));

      var syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(file), path: relativePath);
      var root = syntaxTree.GetRoot();

      foreach (var dynamicType in root.DescendantNodes().OfType<DynamicTypeSyntax>())
        findings.Add(new PolicyFinding("DYNAMIC", relativePath));

      foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
      {
        var methodName = GetInvokedMethodName(invocation.Expression);
        var invokedExpression = invocation.Expression.ToString();

        if (methodName == "CreateInstance" && invokedExpression.Contains("Activator", StringComparison.Ordinal))
          findings.Add(new PolicyFinding("ACTIVATOR", relativePath));

        if (methodName is "MakeGenericType" or "MakeGenericMethod")
          findings.Add(new PolicyFinding("GENERIC_REFLECTION", relativePath));

        if (methodName is "GetTypes" or "GetExportedTypes" or "GetAssemblies")
          findings.Add(new PolicyFinding("ASSEMBLY_SCAN", relativePath));

        if (methodName == "IgnoreQueryFilters")
          findings.Add(new PolicyFinding("QUERY_FILTER_BYPASS", relativePath));

        if (methodName is "FromSql" or "FromSqlRaw" or "FromSqlInterpolated"
          or "ExecuteSql" or "ExecuteSqlAsync"
          or "ExecuteSqlRaw" or "ExecuteSqlRawAsync"
          or "ExecuteSqlInterpolated" or "ExecuteSqlInterpolatedAsync"
          or "SqlQuery" or "SqlQueryAsync" or "SqlQueryRaw" or "SqlQueryRawAsync")
          findings.Add(new PolicyFinding("RAW_SQL", relativePath));
      }
    }

    return findings
      .OrderBy(finding => finding.Rule, StringComparer.Ordinal)
      .ThenBy(finding => finding.Path, StringComparer.Ordinal)
      .ToArray();
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

  private static string FindRepositoryRoot()
  {
    var current = new DirectoryInfo(AppContext.BaseDirectory);

    while (current is not null)
    {
      if (File.Exists(Path.Combine(current.FullName, "Franz.Common.slnx")))
        return current.FullName;

      current = current.Parent;
    }

    throw new DirectoryNotFoundException(
      $"Could not locate Franz.Common.slnx above '{AppContext.BaseDirectory}'.");
  }

  private static string NormalizePath(string path) => path.Replace('\\', '/');

  private sealed record PolicyFinding(string Rule, string Path);

  private sealed record PolicyException(string Rule, string Path, string Reason);
}
