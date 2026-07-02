using team_hub_auth.Services;

namespace team_hub_auth.Tests.Services;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_And_Verify_With_Same_Password_Returns_True()
    {
        const string password = "SuperStrongPassword123!";

        var hash = PasswordHasher.Hash(password);
        var isValid = PasswordHasher.Verify(password, hash);

        Assert.True(isValid);
    }

    [Fact]
    public void Verify_With_Wrong_Password_Returns_False()
    {
        const string password = "SuperStrongPassword123!";
        var hash = PasswordHasher.Hash(password);

        var isValid = PasswordHasher.Verify("WrongPassword123!", hash);

        Assert.False(isValid);
    }
}
