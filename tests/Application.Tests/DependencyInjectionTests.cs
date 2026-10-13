using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Application.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddApplicationBuildsAValidServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        provider.GetRequiredService<IMediator>().ShouldNotBeNull();
    }
}
