using StackSharing.Lib;
using StackSharing.WinApp.IO;
using System;
using System.Security.Cryptography;

namespace StackSharing.WinApp
{
    public sealed class AssemblyConfig : AssemblyConfigRepository<AssemblyConfig>, IConnectionSettings
    {
        public Uri StorageUri { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }

        public void SetPassword(string newPassword)
        {
            this.Password = SecurePasswordHelper.Encrypt(newPassword);
        }

        public string GetPassword()
        {
            return SecurePasswordHelper.Decrypt(this.Password);
        }
    }
}
