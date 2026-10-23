using Application.Abstractions.Authentication;
using Application.Abstractions.Persistence;
using Application.Tests.Support;
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

        // Os handlers dependem de portas implementadas pela Infrastructure; aqui entram fakes de teste.
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IUserRepository, FakeUserRepository>();
        services.AddSingleton<IPasswordHasher, FakePasswordHasher>();
        services.AddSingleton<IAccessTokenService, FakeAccessTokenService>();

        services.AddApplication();

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        provider.GetRequiredService<IMediator>().ShouldNotBeNull();
    }
}
