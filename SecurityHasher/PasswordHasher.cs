namespace SIMS_Assignment.SecurityHasher
{
    public class PasswordHasher : IPasswordHasher
    {
        public string Hash(string password)
        {
            // Implementation for BCrypt/Argon2 goes here
            return "hashed_" + password;
        }

        public bool Verify(string password, string hash)
        {
            return hash == Hash(password);
        }
    }
}
