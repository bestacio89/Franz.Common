using System.Text.Json;
using System.Xml.Linq;
using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReflectionAssembly = System.Reflection.Assembly;
using ArchArchitecture = ArchUnitNET.Domain.Architecture;

namespace Franz.Common.Architecture.Tests;

public sealed class FrameworkArchitectureTests
{
  private const string AdapterNamespacePattern =
    @"^Franz\.Common\.(EntityFramework(\..*)?|MongoDB(\..*)?|CosmosDB(\..*)?|AzureCosmosDB(\..*)?|Http(\..*)?|Grpc(\..*)?|SSO(\..*)?)$";

  private static readonly Lazy<string> RepositoryRoot = new(FindRepositoryRoot);
  private static readonly Lazy<IReadOnlyList<ReflectionAssembly>> RuntimeAssemblies = new(LoadRuntimeAssemblies);
  private static readonly Lazy<ArchArchitecture> ArchitectureModel = new(
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

      IArchRule rule = ArchRuleDefinition
        .Classes()
        .That()
        .ResideInAssembly(assembly!)
        .Should()
        .NotDependOnAnyTypesThat()
        .ResideInNamespaceMatching(AdapterNamespacePattern)
        .Because($"Core package '{packageName}' must depend on abstractions, not infrastructure adapters.");

      rule.Check(ArchitectureModel.Value);
    }
  }

  [Fact]
  public void Core_packages_must_not_reference_infrastructure_adapter_projects_or_packages()
  {
    var corePackages = new HashSet<string>(StringComparer.Ordinal)
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

    var sourceRoot = Path.Combine(RepositoryRoot.Value, "sources");
    var violations = new List<string>();

    foreach (var projectPath in Directory.EnumerateFiles(sourceRoot, "*.csproj", SearchOption.AllDirectories))
    {
      var projectName = Path.GetFileNameWithoutExtension(projectPath);
      if (!corePackages.Contains(projectName))
        continue;

      var document = XDocument.Load(projectPath);
      foreach (var include in document
        .Descendants()
        .Where(element => element.Name.LocalName is "ProjectReference" or "PackageReference")
        .Select(element => (string?)element.Attribute("Include"))
        .Where(value => !string.IsNullOrWhiteSpace(value)))
      {
        var reference = include!.Trim();
        var referencedName = reference.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
          ? Path.GetFileNameWithoutExtension(reference)
          : reference;

        if (IsInfrastructureAdapterProject(referencedName))
          violations.Add($"{projectName} -> {referencedName}");
      }
    }

    Assert.True(
      violations.Count == 0,
      "Core packages must not reference infrastructure adapters:" + Environment.NewLine +
      string.Join(Environment.NewLine, violations));
  }

  private static bool IsInfrastructureAdapterProject(string projectName)
  {
    return projectName.StartsWith("Franz.Common.EntityFramework", StringComparison.Ordinal)
      || projectName is "Franz.Common.MongoDB" or "Franz.Common.CosmosDB" or "Franz.Common.AzureCosmosDB"
      || projectName.StartsWith("Franz.Common.MongoDB.", StringComparison.Ordinal)
      || projectName.StartsWith("Franz.Common.CosmosDB.", StringComparison.Ordinal)
      || projectName.StartsWith("Franz.Common.AzureCosmosDB.", StringComparison.Ordinal)
      || projectName.StartsWith("Franz.Common.Http", StringComparison.Ordinal)
      || projectName.StartsWith("Franz.Common.Grpc", StringComparison.Ordinal)
      || projectName.StartsWith("Franz.Common.SSO", StringComparison.Ordinal)
      || projectName.StartsWith("Franz.Common.Messaging.", StringComparison.Ordinal);
  }

  [Fact]
  public void Grpc_core_must_not_reference_hosting_or_client_transport_implementations()
  {
    var projectPath = Path.Combine(
      RepositoryRoot.Value,
      "sources",
      "Franz.Common.Grpc",
      "Franz.Common.Grpc.csproj");
    var project = XDocument.Load(projectPath);
    var packageReferences = project
      .Descendants()
      .Where(element => element.Name.LocalName == "PackageReference")
      .Select(element => (string?)element.Attribute("Include"))
      .Where(include => !string.IsNullOrWhiteSpace(include))
      .Select(include => include!)
      .ToHashSet(StringComparer.OrdinalIgnoreCase);

    Assert.Contains("Grpc.Core.Api", packageReferences);
    Assert.DoesNotContain("Grpc", packageReferences);
    Assert.DoesNotContain("Grpc.Net.Client", packageReferences);
    Assert.DoesNotContain("Grpc.AspNetCore", packageReferences);
    Assert.DoesNotContain(packageReferences, name =>
      name.StartsWith("Microsoft.AspNetCore.", StringComparison.OrdinalIgnoreCase));

    var assembly = RuntimeAssemblies.Value.Single(candidate =>
      string.Equals(candidate.GetName().Name, "Franz.Common.Grpc", StringComparison.Ordinal));
    var referencedAssemblies = assembly.GetReferencedAssemblies()
      .Select(reference => reference.Name ?? string.Empty)
      .ToArray();

    Assert.Contains("Grpc.Core.Api", referencedAssemblies);
    Assert.DoesNotContain(referencedAssemblies, name =>
      name.StartsWith("Grpc.Net.Client", StringComparison.Ordinal)
      || name.StartsWith("Grpc.AspNetCore", StringComparison.Ordinal)
      || name.StartsWith("Microsoft.AspNetCore.", StringComparison.Ordinal));
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
        .Where(element => element.Name.LocalName is "ProjectReference" or "PackageReference")
        .Select(element => (string?)element.Attribute("Include"))
        .Where(include => !string.IsNullOrWhiteSpace(include)))
      {
        var include = reference!.Trim();
        var referencedName = include.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
          ? Path.GetFileNameWithoutExtension(include)
          : include;

        if (testProjectNames.Contains(referencedName))
          violations.Add($"{projectName} -> {referencedName}");
      }
    }

    Assert.Empty(violations);
  }

  [Fact]
  public void Source_generators_must_target_netstandard20()
  {
    var projectPaths = new[]
    {
      Path.Combine(
        RepositoryRoot.Value,
        "sources",
        "Franz.Common.Mediator.SourceGenerator",
        "Franz.Common.Mediator.SourceGenerator.csproj"),
      Path.Combine(
        RepositoryRoot.Value,
        "sources",
        "Franz.Common.Mapping.Generators",
        "Franz.Common.Mapping.Generators.csproj")
    };

    foreach (var projectPath in projectPaths)
    {
      Assert.True(File.Exists(projectPath), $"Source generator project was not found at '{projectPath}'.");

      var document = XDocument.Load(projectPath);
      var targetFramework = document
        .Descendants()
        .FirstOrDefault(element => element.Name.LocalName == "TargetFramework")
        ?.Value
        .Trim();

      Assert.Equal("netstandard2.0", targetFramework);
    }
  }

  [Fact]
  public void DbContextBase_auditing_uses_direct_auditable_contract_dispatch()
  {
    var contextPath = Path.Combine(
      RepositoryRoot.Value,
      "sources",
      "Franz.Common.EntityFramework",
      "DbContextBase.cs");
    var syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(contextPath));
    var root = syntaxTree.GetRoot();
    var auditMethod = root.DescendantNodes()
      .OfType<MethodDeclarationSyntax>()
      .Single(method => method.Identifier.ValueText == "ApplyAuditing");

    var auditSource = auditMethod.ToFullString();

    Assert.Contains("Entries<IAuditableEntity>", auditSource);
    Assert.DoesNotContain("dynamic", auditSource);
    Assert.DoesNotContain("GetInterfaces", auditSource);
  }

  [Fact]
  public void Tenant_query_filter_policy_rejects_a_context_without_tenant_scoping()
  {
    const string plantedViolation = """
      public sealed class DbContextBase
      {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
          var isTenantEntity = typeof(ITenantEntity).IsAssignableFrom(typeof(Order));
          modelBuilder.Entity<Order>().HasQueryFilter(order => !order.IsDeleted);
        }
      }
      """;

    var findings = TenantIsolationPolicyAnalyzer.Analyze(plantedViolation, "planted/DbContextBase.cs");

    Assert.Contains("TENANT_QUERY_FILTER_MISSING", findings);
  }

  [Fact]
  public void Tenant_isolation_policy_rejects_query_filter_bypass_and_raw_sql()
  {
    const string plantedViolation = """
      public sealed class OrderRepository
      {
        public IQueryable<Order> GetAll(DbContext context)
          => context.Set<Order>().IgnoreQueryFilters();

        public Task<int> DeleteAsync(DbContext context, string sql)
          => context.Database.ExecuteSqlRawAsync(sql);
      }
      """;

    var findings = TenantIsolationPolicyAnalyzer.Analyze(plantedViolation, "planted/OrderRepository.cs");

    Assert.Contains("QUERY_FILTER_BYPASS", findings);
    Assert.Contains("RAW_SQL", findings);
  }

  [Fact]
  public void Tenant_query_filter_policy_accepts_explicit_tenant_scoping()
  {
    const string compliantContext = """
      public sealed class DbContextBase
      {
        public Guid CurrentTenantId => Guid.Empty;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
          foreach (var entityType in modelBuilder.Model.GetEntityTypes())
          {
            var clrType = entityType.ClrType;
            var isTenantEntity = typeof(ITenantEntity).IsAssignableFrom(clrType);
            if (!isTenantEntity)
              continue;

            modelBuilder.Entity(clrType).HasQueryFilter(
              entity => EF.Property<Guid>(entity, nameof(ITenantEntity.TenantId)) == CurrentTenantId);
          }
        }
      }
      """;

    var findings = TenantIsolationPolicyAnalyzer.Analyze(compliantContext, "planted/DbContextBase.cs");

    Assert.DoesNotContain("TENANT_QUERY_FILTER_MISSING", findings);
  }


  [Fact]
  public void Tenant_query_filter_policy_accepts_convention_based_tenant_scoping()
  {
    const string context = """
      public sealed class DbContextBase
      {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
          base.OnModelCreating(modelBuilder);
        }
      }
      """;
    const string convention = """
      internal sealed class TenantQueryFilterConvention
      {
        public void ProcessModelFinalizing()
        {
          var isTenantEntity = typeof(ITenantEntity).IsAssignableFrom(clrType);
          var tenantId = EF.Property<Guid>(parameter, nameof(ITenantEntity.TenantId));
          var currentTenantId = dbContext.CurrentTenantId;
          entityType.SetQueryFilter(Expression.Lambda(
            Expression.Equal(tenantId, currentTenantId), parameter));
        }
      }
      """;

    var findings = TenantIsolationPolicyAnalyzer.Analyze(
      context,
      "planted/DbContextBase.cs",
      convention);

    Assert.DoesNotContain("TENANT_QUERY_FILTER_MISSING", findings);
  }

  [Fact]
  public void Runtime_source_policy_violations_must_be_explicitly_allowlisted()
  {
    var findings = FindRuntimeSourcePolicyViolations();
    var allowlistPath = Path.Combine(
      AppContext.BaseDirectory,
      "architecture-policy-allowlist.json");

    Assert.True(File.Exists(allowlistPath), "The architecture policy allowlist was not copied to the test output.");

    var catalogPath = Path.Combine(
      AppContext.BaseDirectory,
      "source-policy-rule-catalog.json");
    Assert.True(File.Exists(catalogPath), "The source-policy rule catalog was not copied to the test output.");

    var catalog = JsonSerializer.Deserialize<List<PolicyRuleDefinition>>(
      File.ReadAllText(catalogPath),
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

    Assert.NotEmpty(catalog);
    Assert.Equal(catalog.Count, catalog.Select(rule => rule.Id).Distinct(StringComparer.Ordinal).Count());
    Assert.Equal(catalog.Count, catalog.Select(rule => rule.Rule).Distinct(StringComparer.Ordinal).Count());
    Assert.All(catalog, rule =>
    {
      Assert.False(string.IsNullOrWhiteSpace(rule.Id));
      Assert.False(string.IsNullOrWhiteSpace(rule.Rule));
      Assert.False(string.IsNullOrWhiteSpace(rule.Description));
      Assert.Equal("Error", rule.Severity);
    });

    var rulesByName = catalog.ToDictionary(rule => rule.Rule, StringComparer.Ordinal);
    var uncataloguedFindings = findings
      .Where(finding => !rulesByName.ContainsKey(finding.Rule))
      .Select(finding => finding.Rule)
      .Distinct(StringComparer.Ordinal)
      .ToArray();

    Assert.True(
      uncataloguedFindings.Length == 0,
      "Every source-policy finding must have a stable catalog entry:" +
      Environment.NewLine + string.Join(Environment.NewLine, uncataloguedFindings));

    var allowlist = JsonSerializer.Deserialize<List<PolicyException>>(
      File.ReadAllText(allowlistPath),
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

    var invalidRuleIds = allowlist
      .Where(exception =>
        !rulesByName.TryGetValue(exception.Rule, out var rule)
        || !string.Equals(exception.RuleId, rule.Id, StringComparison.Ordinal))
      .Select(exception => $"{exception.Rule}: expected '{(rulesByName.TryGetValue(exception.Rule, out var rule) ? rule.Id : "no catalog entry")}', found '{exception.RuleId}'")
      .ToArray();

    Assert.True(
      invalidRuleIds.Length == 0,
      "Every source-policy exception must reference its rule's stable catalog ID:" +
      Environment.NewLine + string.Join(Environment.NewLine, invalidRuleIds));

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

  private static IReadOnlyList<ReflectionAssembly> LoadRuntimeAssemblies()
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
      .Select(ReflectionAssembly.LoadFrom)
      .GroupBy(assembly => assembly.GetName().Name, StringComparer.Ordinal)
      .Select(group => group.First())
      .ToArray();

    Assert.NotEmpty(assemblies);
    return assemblies;
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
        || relativePath.StartsWith("sources/Franz.Common.Mediator.SourceGenerator/", StringComparison.Ordinal)
        || relativePath.Split('/').Any(segment =>
          segment is "bin" or "obj" || segment.EndsWith(".Tests", StringComparison.OrdinalIgnoreCase)))
        continue;

      if (relativePath.Split('/').Contains("Testing", StringComparer.Ordinal))
        findings.Add(new PolicyFinding("TEST_HELPER_IN_RUNTIME", relativePath));

      var syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(file), path: relativePath);
      var root = syntaxTree.GetRoot();

      var tenantFilterConventionPath = Path.Combine(
        sourceRoot,
        "Franz.Common.EntityFramework",
        "TenantQueryFilterConvention.cs");
      var tenantFilterConventionSource = relativePath == "sources/Franz.Common.EntityFramework/DbContextBase.cs"
        && File.Exists(tenantFilterConventionPath)
          ? File.ReadAllText(tenantFilterConventionPath)
          : null;

      foreach (var rule in TenantIsolationPolicyAnalyzer.Analyze(
        File.ReadAllText(file),
        relativePath,
        tenantFilterConventionSource))
        findings.Add(new PolicyFinding(rule, relativePath));

      var usesDynamicTypes = root
        .DescendantNodes()
        .OfType<IdentifierNameSyntax>()
        .Any(identifier =>
          identifier.Identifier.ValueText == "dynamic"
          && identifier.Parent is TypeSyntax);

      if (usesDynamicTypes)
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

  private sealed record PolicyRuleDefinition(string Id, string Rule, string Severity, string Description);

  private sealed record PolicyException(string Rule, string RuleId, string Path, string Reason);
}
