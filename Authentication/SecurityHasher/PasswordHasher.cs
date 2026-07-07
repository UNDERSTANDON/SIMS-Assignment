using System.Security.Cryptography;
using System.Text;

namespace SIMS_Assignment.Authentication.SecurityHasher
{
    public class PasswordHasher : IPasswordHasher
    {
        public string Hash(string password)
        {
            // SHA256
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public bool Verify(string password, string hash)
        {
            return hash == Hash(password);
        }
    }
}
