using NetArchTest.Rules;

namespace yggdrasil.Architecture.Tests;

/// <summary>Contracts are the public API of a module: messages and DTOs only, no implementation dependencies.</summary>
public sealed class ContractsPurityTests
{
    [Theory]
    [InlineData("Microsoft.EntityFrameworkCore")]
    [InlineData("Microsoft.AspNetCore")]
    [InlineData("FluentValidation")]
    [InlineData("yggdrasil.Web")]
    [InlineData("yggdrasil.Persistence")]
    public void ContractsDoNotDependOn(string dependency)
    {
        var violations = Solution.ContractsAssemblies
            .Select(assembly => Types.InAssembly(assembly).ShouldNot().HaveDependencyOn(dependency).GetResult())
            .Where(static result => !result.IsSuccessful)
            .SelectMany(static result => result.FailingTypeNames ?? []);

        Assert.Empty(violations);
    }
}
