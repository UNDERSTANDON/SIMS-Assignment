namespace SIMS_Assignment.Models
{
    public abstract class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        // Constructor
        public User(int id, string name, string role, string passwordHash)
        {
            Id = id;
            Name = name;
            Role = role;
            PasswordHash = passwordHash;
        }
        public User() { }
    }
}
