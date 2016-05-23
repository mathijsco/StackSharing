using System;

namespace StackSharing.Lib
{
    public interface IConnectionSettings
    {
       Uri StorageUri { get; }

       string UserName { get; }

       string GetPassword();
    }
}
