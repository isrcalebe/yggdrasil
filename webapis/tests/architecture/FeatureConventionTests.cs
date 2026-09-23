using FluentValidation;
using Mediator;

namespace yggdrasil.Architecture.Tests;

/// <summary>Conventions of the vertical slices in <c>Features/v{version}/{Area}/{Feature}/</c>.</summary>
public sealed class FeatureConventionTests
{
    private static readonly Type[] handler_interfaces =
    [
        typeof(ICommandHandler<,>),
        typeof(IQueryHandler<,>),
        typeof(INotificationHandler<>),
    ];

    private static IEnumerable<Type> moduleTypes
        => Solution.ModuleAssemblies.SelectMany(static assembly => assembly.GetTypes()).Where(static type => !type.IsCompilerGenerated());

    [Fact]
    public void HandlersArePublicSealedAndLiveInFeatures()
    {
        var violations = moduleTypes
            .Where(static type => type.GetInterfaces().Any(static contract =>
                contract.IsGenericType && handler_interfaces.Contains(contract.GetGenericTypeDefinition())))
            .Where(static type => !type.IsPublic || !type.IsSealed || !type.IsInFeatures() || !type.Name.EndsWith("Handler", StringComparison.Ordinal))
            .Select(static type => type.FullName);

        Assert.Empty(violations);
    }

    [Fact]
    public void ValidatorsArePublicSealedAndLiveInFeatures()
    {
        var violations = moduleTypes
            .Where(static type => typeof(IValidator).IsAssignableFrom(type) && !type.IsAbstract)
            .Where(static type => !type.IsPublic || !type.IsSealed || !type.IsInFeatures() || !type.Name.EndsWith("Validator", StringComparison.Ordinal))
            .Select(static type => type.FullName);

        Assert.Empty(violations);
    }

    [Fact]
    public void FeatureTypesAreHandlersValidatorsOrEndpoints()
    {
        var violations = moduleTypes
            .Where(static type => type.IsInFeatures() && !type.IsNested)
            .Where(static type => !type.Name.EndsWith("Handler", StringComparison.Ordinal)
                && !type.Name.EndsWith("Validator", StringComparison.Ordinal)
                && !(type.Name.EndsWith("Endpoint", StringComparison.Ordinal) && type.IsAbstract && type.IsSealed))
            .Select(static type => type.FullName);

        Assert.Empty(violations);
    }
}

internal static class TypeExtensions
{
    extension(Type self)
    {
        public bool IsInFeatures()
            => self.Namespace?.Contains(".Features.v", StringComparison.Ordinal) ?? false;

        public bool IsCompilerGenerated()
            => self.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), inherit: false)
                || self.Name.Contains('<', StringComparison.Ordinal);
    }
}
