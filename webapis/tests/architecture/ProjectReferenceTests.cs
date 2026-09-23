namespace yggdrasil.Architecture.Tests;

public sealed class ProjectReferenceTests
{
    [Fact]
    public void ModulesDoNotReferenceOtherModuleRuntimes()
    {
        var violations = Solution.ModuleProjects
            .Where(static project => Solution.IsModuleRuntime(project.Name))
            .SelectMany(static project => project.References
                .Where(reference => Solution.IsModuleRuntime(reference) && reference != project.Name)
                .Select(reference => $"{project.Name} -> {reference}"));

        Assert.Empty(violations);
    }

    [Fact]
    public void ContractsOnlyReferenceCoreOrOtherContracts()
    {
        var violations = Solution.ModuleProjects
            .Where(static project => Solution.IsContracts(project.Name))
            .SelectMany(static project => project.References
                .Where(static reference => reference != "yggdrasil.Core" && !Solution.IsContracts(reference))
                .Select(reference => $"{project.Name} -> {reference}"));

        Assert.Empty(violations);
    }

    [Fact]
    public void BuildingBlocksDoNotReferenceModules()
    {
        var violations = Solution.BuildingBlockProjects
            .SelectMany(static project => project.References
                .Where(static reference => reference.StartsWith("yggdrasil.Modules.", StringComparison.Ordinal))
                .Select(reference => $"{project.Name} -> {reference}"));

        Assert.Empty(violations);
    }

    [Fact]
    public void CoreHasNoProjectReferences()
    {
        var core = Assert.Single(Solution.BuildingBlockProjects, static project => project.Name == "yggdrasil.Core");

        Assert.Empty(core.References);
    }
}
