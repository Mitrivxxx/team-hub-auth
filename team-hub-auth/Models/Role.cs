namespace team_hub_auth.Models;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public ICollection<User> Users { get; set; } = [];
}
