using team_hub_auth.Dtos;
using team_hub_auth.Validators;

namespace team_hub_auth.Tests.Validators;

public class UpdateMeRequestValidatorTests
{
    readonly UpdateMeRequestValidator validator = new();

    [Fact]
    public void Validate_WhenNameIsValid_ShouldPass()
    {
        var result = validator.Validate(new UpdateMeRequest { Name = "Alice" });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenNoFieldsProvided_ShouldFail()
    {
        var result = validator.Validate(new UpdateMeRequest());

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenEmailIsInvalid_ShouldFail()
    {
        var result = validator.Validate(new UpdateMeRequest { Email = "not-an-email" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateMeRequest.Email));
    }

    [Fact]
    public void Validate_WhenNameIsTooShort_ShouldFail()
    {
        var result = validator.Validate(new UpdateMeRequest { Name = "A" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateMeRequest.Name));
    }
}

public class ChangeMyPasswordRequestValidatorTests
{
    readonly ChangeMyPasswordRequestValidator validator = new();

    [Fact]
    public void Validate_WhenPasswordsAreValid_ShouldPass()
    {
        var result = validator.Validate(new ChangeMyPasswordRequest
        {
            CurrentPassword = "old-secret",
            NewPassword = "newsecret1"
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenNewPasswordTooShort_ShouldFail()
    {
        var result = validator.Validate(new ChangeMyPasswordRequest
        {
            CurrentPassword = "old-secret",
            NewPassword = "short"
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ChangeMyPasswordRequest.NewPassword));
    }
}
