﻿using StackSharing.Lib.Models;
using System;
using System.Collections.Generic;
using System.Web;

namespace StackSharing.Lib
{
    internal static class OnlinePathBuilder
    {
        // The base URL for all calls to the share API is: <owncloud_base_url>/ocs/v1.php/apps/files_sharing/api/v1
        private const string BaseUrl = "ocs/v1.php/apps/files_sharing/api/v1";
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
            {
                return new Uri(SanitizeHost(host) + RootPath, UriKind.Absolute);
            }

            return new Uri(SanitizeHost(host) + RootPath + "/" + location.TrimStart('/'), UriKind.Absolute);
        }

        public static OnlineItem CreateChild(OnlineItem parent, string child)
        {
            if (string.IsNullOrEmpty(child))
            {
                throw new ArgumentException("The child cannot be null or empty.", nameof(child));
            }

            return new OnlineItem
            {
                LocalPath = (parent.LocalPath + "/" + child).TrimStart('/'),
                FullPath = new Uri(parent.FullPath + "/" + child)
            };
        }

        public static Uri CreateFileShareUri(Uri host, IDictionary<string, string> queryValues = null)
        {
            string uri = SanitizeHost(host) + BaseUrl + "/shares?format=json";

            if (queryValues != null)
            {
                foreach (var queryValue in queryValues)
                {
                    uri += $"&{queryValue.Key}={queryValue.Value}";
                }
            }

            return new Uri(uri, UriKind.Absolute);
        }

        public static Uri CreateFileExpirationOrPasswordUri(Uri host, SharedOnlineItem sharedItem)
        {
            return new Uri(SanitizeHost(host) + BaseUrl + "/shares/" + sharedItem.ShareId + "?format=json", UriKind.Absolute);
        }

        private static string SanitizeHost(Uri host)
        {
            string leftpath = host.GetLeftPart(UriPartial.Path);
            return leftpath.EndsWith("/") ? leftpath : leftpath + "/";
        }
    }
}