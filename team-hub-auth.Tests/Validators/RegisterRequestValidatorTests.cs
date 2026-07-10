using team_hub_auth.Dtos;
using team_hub_auth.Validators;

namespace team_hub_auth.Tests.Validators;

public class RegisterRequestValidatorTests
{
    readonly RegisterRequestValidator validator = new();

    [Fact]
    public void Validate_WhenRequestIsValid_ShouldPass()
    {
        var request = new RegisterRequest
        {
            Username = "john_doe_1",
            Name = "John",
            Surname = "Doe",
            Password = "secret123456"
        };

        var result = validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    public void Validate_WhenUsernameIsTooShortOrEmpty_ShouldFail(string username)
    {
        var request = ValidRequest() with { Username = username };

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterRequest.Username));
    }

    [Fact]
    public void Validate_WhenUsernameContainsForbiddenCharacters_ShouldFail()
    {
        var request = ValidRequest() with { Username = "john@doe" };

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterRequest.Username));
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    public void Validate_WhenPasswordIsInvalid_ShouldFail(string password)
    {
        var request = ValidRequest() with { Password = password };

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterRequest.Password));
    }

    [Theory]
    [InlineData("name", "")]
    [InlineData("", "surname")]
    public void Validate_WhenNameOrSurnameIsEmpty_ShouldFail(string name, string surname)
    {
        var request = ValidRequest() with { Name = name, Surname = surname };

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName is nameof(RegisterRequest.Name) or nameof(RegisterRequest.Surname));
    }

    static RegisterRequestRecord ValidRequest() => new(
        Username: "john_doe",
        Name: "John",
        Surname: "Doe",
        Password: "secret123456");

    readonly record struct RegisterRequestRecord(
        string Username,
        string Name,
        string Surname,
        string Password)
    {
        public static implicit operator RegisterRequest(RegisterRequestRecord record) =>
            new()
            {
                Username = record.Username,
                Name = record.Name,
                Surname = record.Surname,
                Password = record.Password
            };
    }
}
