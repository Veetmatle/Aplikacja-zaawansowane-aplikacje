using FluentAssertions;
using NetArchTest.Rules;
using ShopApp.Application.Interfaces;
using ShopApp.Core.Common;

namespace ShopApp.ArchTests;

/// <summary>
/// Architecture tests that verify the Dependency Rule is maintained.
/// Core → Application → Infrastructure → API (only inward dependencies).
/// </summary>
public class DependencyTests
{
    private const string CoreNamespace = "ShopApp.Core";
    private const string ApplicationNamespace = "ShopApp.Application";
    private const string InfrastructureNamespace = "ShopApp.Infrastructure";
    private const string ApiNamespace = "ShopApp.API";

    [Fact]
    public void Core_ShouldNotDependOn_Application()
    {
        var result = Types.InAssembly(typeof(BaseEntity).Assembly)
            .ShouldNot()
            .HaveDependencyOn(ApplicationNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Core must not depend on Application (Dependency Rule violation)");
    }

    [Fact]
    public void Core_ShouldNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(typeof(BaseEntity).Assembly)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Core must not depend on Infrastructure (Dependency Rule violation)");
    }

    [Fact]
    public void Core_ShouldNotDependOn_API()
    {
        var result = Types.InAssembly(typeof(BaseEntity).Assembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Core must not depend on API (Dependency Rule violation)");
    }

    [Fact]
    public void Application_ShouldNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(typeof(IAuthService).Assembly)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Application must not depend on Infrastructure (Dependency Rule violation)");
    }

    [Fact]
    public void Application_ShouldNotDependOn_API()
    {
        var result = Types.InAssembly(typeof(IAuthService).Assembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Application must not depend on API (Dependency Rule violation)");
    }

    [Fact]
    public void Infrastructure_ShouldNotDependOn_API()
    {
        var result = Types.InAssembly(typeof(ShopApp.Infrastructure.Data.AppDbContext).Assembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Infrastructure must not depend on API (Dependency Rule violation)");
    }
}
