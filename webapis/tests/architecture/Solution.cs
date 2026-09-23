using System.Reflection;
using System.Xml.Linq;

namespace yggdrasil.Architecture.Tests;

/// <summary>Locates projects and assemblies of the solution. New modules are picked up without changes here.</summary>
internal static class Solution
{
    private const string modules_prefix = "yggdrasil.Modules.";

    private const string contracts_suffix = ".Contracts";

    public static DirectoryInfo Root { get; } = findRoot();

    public static IReadOnlyList<Project> ModuleProjects { get; } = findProjects("modules");

    public static IReadOnlyList<Project> BuildingBlockProjects { get; } = findProjects("buildingblocks");

    public static IReadOnlyList<Assembly> ModuleAssemblies { get; } = loadAssemblies(contracts: false);

    public static IReadOnlyList<Assembly> ContractsAssemblies { get; } = loadAssemblies(contracts: true);

    public static bool IsContracts(string name) => name.EndsWith(contracts_suffix, StringComparison.Ordinal);

    public static bool IsModuleRuntime(string name) => name.StartsWith(modules_prefix, StringComparison.Ordinal) && !IsContracts(name);

    private static DirectoryInfo findRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (directory.GetFiles("yggdrasil.slnx").Length > 0)
                return directory;
        }

        throw new InvalidOperationException("Unable to locate the directory containing yggdrasil.slnx.");
    }

    private static Project[] findProjects(string folder)
    {
        var directory = Path.Combine(Root.FullName, folder);

        if (!Directory.Exists(directory))
            return [];

        return [.. Directory
            .GetFiles(directory, "*.csproj", SearchOption.AllDirectories)
            .Select(static path => new Project(
                Path.GetFileNameWithoutExtension(path),
                [.. XDocument.Load(path)
                    .Descendants("ProjectReference")
                    // MSBuild paths use '\', which is not a directory separator on Linux: normalize it first.
                    .Select(static reference => Path.GetFileNameWithoutExtension(((string?)reference.Attribute("Include") ?? string.Empty).Replace('\\', '/')))]))];
    }

    private static Assembly[] loadAssemblies(bool contracts)
        => [.. Directory
            .GetFiles(AppContext.BaseDirectory, $"{modules_prefix}*.dll")
            .Select(static path => Path.GetFileNameWithoutExtension(path))
            .Where(name => IsContracts(name) == contracts)
            .Select(static name => Assembly.Load(name))];
}

internal sealed record Project(string Name, IReadOnlyList<string> References);
