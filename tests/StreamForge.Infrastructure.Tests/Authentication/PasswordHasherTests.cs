using StreamForge.Infrastructure.Authentication;

namespace StreamForge.Infrastructure.Tests.Authentication;

public sealed class PasswordHasherTests
{
    [Fact]
    public void Hash_CreatesSaltedHashThatCanBeVerified()
    {
        const string password = "Correct Horse Battery Staple";

        var firstHash = PasswordHasher.Hash(password);
        var secondHash = PasswordHasher.Hash(password);

        Assert.NotEqual(firstHash, secondHash);
        Assert.True(PasswordHasher.Verify(password, firstHash));
        Assert.True(PasswordHasher.Verify(password, secondHash));
        Assert.False(PasswordHasher.Verify("wrong password", firstHash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("PBKDF2$100000$not-base64$not-base64")]
    public void Verify_ReturnsFalseForInvalidHash(string passwordHash)
    {
        Assert.False(PasswordHasher.Verify("password", passwordHash));
    }
}
