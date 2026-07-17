using team_hub_auth.Services.Password;

namespace team_hub_auth.Tests.Services;

public class PasswordHasherTests
{
    readonly IPasswordHasher hasher = new PasswordHasher();

    [Fact]
    public void Hash_And_Verify_With_Same_Password_Returns_True()
    {
        const string password = "SuperStrongPassword123!";

        var hash = hasher.Hash(password);
        var isValid = hasher.Verify(password, hash);

        Assert.True(isValid);
    }

    [Fact]
    public void Verify_With_Wrong_Password_Returns_False()
    {
        const string password = "SuperStrongPassword123!";
        var hash = hasher.Hash(password);

        var isValid = hasher.Verify("WrongPassword123!", hash);

        Assert.False(isValid);
    }
}
