namespace Ekiphan.UnitTests.Architecture;

public sealed class LayerDependencyTests
{
    [Fact]
    public void DomainDoesNotReferenceApplicationOrInfrastructure()
    {
        var referencedAssemblies = Domain.AssemblyReference.Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name)
            .ToArray();

        Assert.DoesNotContain("Ekiphan.Application", referencedAssemblies);
        Assert.DoesNotContain("Ekiphan.Infrastructure", referencedAssemblies);
    }
}
