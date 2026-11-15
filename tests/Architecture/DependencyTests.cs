using System.Reflection;
using Contas.Application.Accounts.Commands;
using Contas.Domain;
using Contas.Infrastructure.Persistence;
using Xunit;

public sealed class DependencyTests
{
    [Fact]
    public void DomainUsesOnlyFrameworkAssemblies()
        => Assert.All(typeof(Account).Assembly.GetReferencedAssemblies(),
            reference => Assert.True(reference.Name?.StartsWith("System.", StringComparison.Ordinal), reference.FullName));

    [Fact]
    public void ApplicationDependsOnlyOnDomainFrameworkAndMediatorAbstractions()
        => Assert.All(typeof(OpenAccountCommand).Assembly.GetReferencedAssemblies(),
            reference => Assert.True(reference.Name is "Contas.Domain" or "Mediator" ||
                reference.Name?.StartsWith("System.", StringComparison.Ordinal) == true, reference.FullName));

    [Fact]
    public void InfrastructureDoesNotDependOnApi()
        => Assert.DoesNotContain(typeof(AccountsDb).Assembly.GetReferencedAssemblies(),
            reference => reference.Name is "Contas.Api" || reference.Name?.StartsWith("Microsoft.AspNetCore") == true);

    [Fact]
    public void ProjectGraphOnlyAllowsInwardDependencies()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Directory.Packages.props")))
            root = root.Parent;
        Assert.NotNull(root);
        var allowed = new Dictionary<string, string[]>
        {
            ["Contas.Domain"] = [],
            ["Contas.Application"] = ["Contas.Domain"],
            ["Contas.Infrastructure"] = ["Contas.Application"],
            ["Contas.Api"] = ["Contas.Application", "Contas.Infrastructure"],
        };
        foreach (var (layer, dependencies) in allowed)
        {
            var project = System.Xml.Linq.XDocument.Load(Path.Combine(root.FullName, "src", layer, layer + ".csproj"));
            var actual = project.Descendants("ProjectReference").Select(node =>
                Path.GetFileNameWithoutExtension((string?)node.Attribute("Include"))).Order().ToArray();
            Assert.Equal(dependencies.Order().ToArray(), actual);
        }
    }
}
