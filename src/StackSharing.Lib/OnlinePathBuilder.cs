using StackSharing.Lib.Models;
using System;

namespace StackSharing.Lib
{
    internal static class OnlinePathBuilder
    {
        private const string RootPath = "remote.php/webdav";

        /// <summary>
        /// Converts a local path to an URL on your Stack account for WebDAV.
        /// </summary>
        /// <param name="host">The URL to the stack account.</param>
        /// <param name="location">The local path. E.g. /MyFolder/MyFile.zip</param>
        /// <returns>URI to the location on the WebDAV.</returns>
        public static Uri ConvertPathToFullUri(Uri host, string location)
        {
            if (location == null)
                return new Uri(SanitizeHost(host) + RootPath, UriKind.Absolute);
            return new Uri(SanitizeHost(host) + RootPath + "/" + location.TrimStart('/'), UriKind.Absolute);
        }

        public static OnlineItem CreateChild(OnlineItem parent, string child)
        {
            if (string.IsNullOrEmpty(child)) throw new ArgumentException("The child cannot be null or empty.", "child");

            return new OnlineItem
            {
                LocalPath = (parent.LocalPath + "/" + child).TrimStart('/'),
                FullPath = new Uri(parent.FullPath + "/" + child)
            };
        }

        public static Uri FileShareApi(Uri host)
        {
            return new Uri(SanitizeHost(host) + "ocs/v1.php/apps/files_sharing/api/v1/shares?format=json", UriKind.Absolute);
        }

        public static Uri FileExpirationApi(Uri host, SharedOnlineItem sharedItem)
        {
            return new Uri(SanitizeHost(host) + "ocs/v1.php/apps/files_sharing/api/v1/shares/" + sharedItem.ShareId + "?format=json", UriKind.Absolute);
        }

        private static string SanitizeHost(Uri host)
        {
            return host.GetLeftPart(UriPartial.Path) + "/";
        }
    }
}
