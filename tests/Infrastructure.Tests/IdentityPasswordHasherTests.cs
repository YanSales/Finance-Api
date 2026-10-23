using System.Buffers.Binary;
using Infrastructure.Authentication;
using Shouldly;

namespace Infrastructure.Tests;

public sealed class IdentityPasswordHasherTests
{
    private const string Password = "CorrectHorse-Battery-2026";

    private readonly IdentityPasswordHasher hasher = new();

    [Fact]
    public void HashNeverContainsThePlainPasswordAndIsSalted()
    {
        var first = hasher.Hash(Password);
        var second = hasher.Hash(Password);

        first.ShouldNotContain(Password);
        first.ShouldNotBe(second);
    }

    [Fact]
    public void HashUsesTheIdentityV3FormatWithTheConfiguredIterationCount()
    {
        var bytes = Convert.FromBase64String(hasher.Hash(Password));

        bytes[0].ShouldBe((byte)0x01);
        BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(5, 4)).ShouldBe((uint)IdentityPasswordHasher.IterationCount);
    }

    [Fact]
    public void VerifyAcceptsTheCorrectPassword()
    {
        hasher.Verify(hasher.Hash(Password), Password).ShouldBeTrue();
    }

    [Fact]
    public void VerifyRejectsAnIncorrectPassword()
    {
        hasher.Verify(hasher.Hash(Password), "another-password-123").ShouldBeFalse();
    }

    [Fact]
    public void VerifyRejectsAnyPasswordWhenThereIsNoHash()
    {
        hasher.Verify(null, Password).ShouldBeFalse();
    }
}
