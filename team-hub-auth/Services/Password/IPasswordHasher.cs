namespace team_hub_auth.Services.Password;

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);
}
