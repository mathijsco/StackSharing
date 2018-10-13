using Newtonsoft.Json;
using StackSharing.Lib.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using StackSharing.Lib.Models.OwnCloud;
using WebDav;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace StackSharing.Lib
{
    public class StackClient
    {
        private readonly WebDavClient _client;
        private readonly IConnectionSettings _connectionSettings;
        private readonly CancellationToken _cancellationToken;
        private readonly IHttpClientFactory _httpClientFactory;

        public StackClient(IConnectionSettings connectionSettings, CancellationToken cancellationToken)
        {
            _connectionSettings = connectionSettings;
            _cancellationToken = cancellationToken;
            _httpClientFactory = new HttpClientFactory(_connectionSettings);

            _client = new WebDavClient(new WebDavClientParams
            {
                Credentials = new NetworkCredential(_connectionSettings.UserName, _connectionSettings.GetPassword()),
                UseDefaultCredentials = false,
                Timeout = TimeSpan.FromHours(1)
            });
        }

        public async Task<OnlineItem> CreateFolderAsync(string relativePath, OnlineItem parent = null)
        {
            // The default relative path is just nothing.
            if (string.IsNullOrEmpty(relativePath))
                throw new ArgumentException("The relative path cannot be null or empty.", nameof(relativePath));
            if (relativePath.StartsWith("/") || relativePath.EndsWith("/"))
                throw new ArgumentException("The relative path cannot start or end with a slash.", nameof(relativePath));

            // Create a new root if it does not exist (this will be the /Share/Guid/ folder.
            if (parent == null)
            {
                parent = new OnlineItem
                {
                    LocalPath = string.Empty,
                    FullPath = OnlinePathBuilder.ConvertPathToFullUri(_connectionSettings.StorageUri, null)
                };
            }

            OnlineItem currentChild = parent;

            var pathParts = relativePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string pathPart in pathParts)
            {
                currentChild = OnlinePathBuilder.CreateChild(currentChild, pathPart);

                // Create the folder
                _cancellationToken.ThrowIfCancellationRequested();

                var createResponse = await _client.Mkcol(currentChild.FullPath, new MkColParameters { CancellationToken = _cancellationToken }).ConfigureAwait(false);
                if (createResponse.StatusCode != 201 && createResponse.StatusCode != 405) // 405 if maybe a tweak to not check if it exists?
                {
                    throw new Exception($"The folder is not created, code={createResponse.StatusCode}, description={createResponse.Description}");
                }
            }

            return OnlinePathBuilder.CreateChild(parent, relativePath);
        }

        public UploadStatus UploadFiles(OnlineItem parentFolder, IList<string> filePaths)
        {
            var status = new UploadStatus();

            status.Task = Task.Run(async () =>
            {
                var singleItem = await UploadFilesAsync(parentFolder, filePaths, status);
                status.Result = singleItem ?? parentFolder;
            }, _cancellationToken);

            return status;
        }

        private async Task<OnlineItem> UploadFilesAsync(OnlineItem parentFolder, IList<string> filePaths, UploadStatus status)
        {
            status.TotalFiles += filePaths.Count;

            OnlineItem lastItem = null;
            foreach (var path in filePaths)
            {
                status.FileProgress = 0f;
                status.CurrentFileNo++;

                // If the path is a directory, create a new folder and upload the content.
                if (Directory.Exists(path))
                {
                    lastItem = await CreateFolderAsync(Path.GetFileName(path), parentFolder);
                    await UploadFilesAsync(lastItem, Directory.GetFileSystemEntries(path), status);
                }
                // Upload the file if it exists.
                else if (File.Exists(path))
                {
                    lastItem = UploadFile(status, parentFolder, path);
                }

                _cancellationToken.ThrowIfCancellationRequested();
            }

            // make sure to return this single file.
            return filePaths.Count == 1 ? lastItem : null;
        }

        private OnlineItem UploadFile(UploadStatus status, OnlineItem folder, string filePath)
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                long length = stream.Length;

                var child = OnlinePathBuilder.CreateChild(folder, Path.GetFileName(filePath));
                var task = _client.PutFile(child.FullPath, stream, new PutFileParameters { CancellationToken = _cancellationToken });
                do
                {
                    status.FileProgress = (float)((double)stream.Position / length);
                } while (!task.IsFaulted && !task.IsCanceled && !task.IsCompleted && !task.Wait(100));

                return child;
            }
        }

        /// <summary>
        /// https://doc.owncloud.org/server/10.0/developer_manual/core/ocs-share-api.html#create-a-new-share
        /// </summary>
        /// <param name="onlineItem">The OnlineItem.</param>
        /// <returns>SharedOnlineItem</returns>
        public async Task<SharedOnlineItem> CreateShareAsync(OnlineItem onlineItem)
        {
            // https://doc.owncloud.org/server/10.0/developer_manual/core/ocs-share-api.html#function-arguments
            var keyValuePairs = new Dictionary<string, string>
            {
                // path - (string) path to the file/folder which should be shared
                { "path", @"/" + onlineItem.LocalPath },

                // shareType - (int) 0 = user; 1 = group; 3 = public link; 6 = federated cloud share
                { "shareType", "3" },

                // permissions - (int) 1 = read; 2 = update; 4 = create; 8 = delete; 16 = share; 31 = all (default: 31, for public shares: 1)
                { "permissions", "1" }
            };

            var uri = OnlinePathBuilder.CreateFileShareUri(_connectionSettings.StorageUri);
            await SetPropertiesAsync(uri, keyValuePairs);
            // {"ocs":{"meta":{"status":"ok","statuscode":100,"message":null},"data":{"id":0,"url":"https://???.stackstorage.com/index.php/s/M56HdvsvIAhi37Z","token":"M56HdvsvIAhi37Z","permissions":1,"expiration":""}}}

            // TODO : The ID is not returned anymore, always value 0 ? So we need to get all SharesAsync.
            return await GetSharesAsync(onlineItem);
        }

        /// <summary>
        /// https://doc.owncloud.org/server/10.0/developer_manual/core/ocs-share-api.html#get-shares-from-a-specific-file-or-folder
        /// </summary>
        /// <param name="onlineItem">The OnlineItem.</param>
        /// <returns>OwnCloudResponse</returns>
        private async Task<SharedOnlineItem> GetSharesAsync(OnlineItem onlineItem)
        {
            var keyValuePairs = new Dictionary<string, string>
            {
                // path - (string) path to the file/folder which should be shared
                // TODO : however this does not work? No data returned, so for now, just return all shares.
                // { "path", @"/" + onlineItem.LocalPath },

                // returns not only the shares from the current user but all shares from the given file.
                { "reshares", "true" }
            };

            var uri = OnlinePathBuilder.CreateFileShareUri(_connectionSettings.StorageUri, keyValuePairs);

            HttpResponseMessage response = await _httpClientFactory.GetHttpClient().GetAsync(uri, _cancellationToken);

            string content = await response.Content.ReadAsStringAsync();
            var cloudResponse = JsonConvert.DeserializeObject<OwnCloudResponse>(content);
            var items = (JArray)cloudResponse.OCS.Data;

            var item = items.Single(d => d["path"].Value<string>() == @"/" + onlineItem.LocalPath);

            return new SharedOnlineItem(onlineItem)
            {
                ShareId = item["id"].Value<string>(),
                ShareUrl = new Uri(item["url"].Value<string>(), UriKind.Absolute)
            };
        }

        public async Task<OwnCloudResponse> SetExpiryDateAsync(SharedOnlineItem onlineItem, DateTime limit)
        {
            var keyValuePairs = new Dictionary<string, string>
            {
                { "expireDate", limit.ToString("yyyy-MM-dd") }
            };

            return await UpdateShareAsync(OnlinePathBuilder.CreateFileExpirationOrPasswordUri(_connectionSettings.StorageUri, onlineItem), keyValuePairs);
            // {"ocs":{"meta":{"status":"ok","statuscode":100,"message":null},"data":[]}}
        }

        public async Task<OwnCloudResponse> SetPasswordAsync(SharedOnlineItem onlineItem, string password)
        {
            var keyValuePairs = new Dictionary<string, string>
            {
                { "password", password }
            };

            return await UpdateShareAsync(OnlinePathBuilder.CreateFileExpirationOrPasswordUri(_connectionSettings.StorageUri, onlineItem), keyValuePairs);
            // {"ocs":{"meta":{"status":"ok","statuscode":100,"message":null},"data":[]}}
        }

        private Task<OwnCloudResponse> SetPropertiesAsync(Uri uri, IDictionary<string, string> content)
        {
            return HandlePropertiesAsync(uri, content, "POST");
        }

        /// <summary>
        /// https://doc.owncloud.org/server/10.0/developer_manual/core/ocs-share-api.html#update-share
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        private Task<OwnCloudResponse> UpdateShareAsync(Uri uri, IDictionary<string, string> content)
        {
            return HandlePropertiesAsync(uri, content, "PUT");
        }

        private async Task<OwnCloudResponse> HandlePropertiesAsync(Uri uri, IDictionary<string, string> content, string method)
        {
            var form = new FormUrlEncodedContent(content);

            HttpResponseMessage response;
            if (method == "POST")
            {
                response = await _httpClientFactory.GetHttpClient().PostAsync(uri, form, _cancellationToken);
            }
            else
            {
                response = await _httpClientFactory.GetHttpClient().PutAsync(uri, form, _cancellationToken);
            }

            string data = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<OwnCloudResponse>(data);
        }
    }
}