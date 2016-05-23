using System.Security.Cryptography;
using System.Text;

namespace StackSharing.Lib.Utilities
{
    public static class PasswordGenerator
    {
        private const int Length = 16;

        public static string Generate()
        {
            var buffer = new byte[100];
            var rng = new RNGCryptoServiceProvider();
            var builder = new StringBuilder();

            do
            {
                rng.GetNonZeroBytes(buffer);
                foreach (var b in buffer)
                {
                    // TODO: Make the change for numbers equivalent to the characters. (10 numbers vs 52 characters)
                    if (b >= '0' && b <= '9' || b >= 'a' && b <= 'z' || b >= 'A' && b <= 'Z')
                        builder.Append((char)b);
                }

            } while (builder.Length < Length);

            return builder.ToString(0, Length);
        }
    }
}
