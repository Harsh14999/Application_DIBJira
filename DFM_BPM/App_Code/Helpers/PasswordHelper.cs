using System;
using System.Security.Cryptography;
using System.Text;

namespace DFM_BPM.App_Code.Helpers
{
    public static class PasswordHelper
    {
        public static string GenerateSalt()
        {
            var rng = new RNGCryptoServiceProvider();
            byte[] b = new byte[32];
            rng.GetBytes(b);
            return Convert.ToBase64String(b);
        }

        public static string Hash(string password, string salt)
        {
            using (var sha = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(password + salt);
                byte[] hash  = sha.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
}
