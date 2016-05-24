using Newtonsoft.Json;
using StackSharing.Lib.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using StackSharing.Lib.Models.OwnCloud;
using WebDav;

namespace StackSharing.Lib
{
    public class StackClient
    {
        private readonly WebDavClient _client;
        private readonly IConnectionSettings _connectionSettings;
        private readonly CancellationToken _cancellationToken;

        public StackClient(IConnectionSettings connectionSettings, CancellationToken cancellationToken)
        {
            _connectionSettings = connectionSettings;
            _cancellationToken = cancellationToken;

            _client = new WebDavClient(new WebDavClientParams
            {
                Credentials = new NetworkCredential(_connectionSettings.UserName, _connectionSettings.GetPassword()),
                UseDefaultCredentials = false
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
                parent = new OnlineItem();
                parent.LocalPath = string.Empty;
                parent.FullPath = OnlinePathBuilder.ConvertPathToFullUri(_connectionSettings.StorageUri, null);
            }

            OnlineItem currentChild = parent;

            var pathParts = relativePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string pathPart in pathParts)
            {
                currentChild = OnlinePathBuilder.CreateChild(currentChild, pathPart);

                // Create the folder
                _cancellationToken.ThrowIfCancellationRequested();

                var createResponse = await _client.Mkcol(currentChild.FullPath, new MkColParameters { CancellationToken = _cancellationToken });
                if (createResponse.StatusCode != 201 && createResponse.StatusCode != 405) // 405 if maybe a tweak to not check if it exists?
                    throw new Exception("The folder is not created because of some unknown reason, but we got the following description back: " + createResponse.Description);
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
            if (filePaths.Count == 1)
                return lastItem;
            return null;
        }

        private OnlineItem UploadFile(UploadStatus status, OnlineItem folder, string filePath)
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                var length = stream.Length;

                var child = OnlinePathBuilder.CreateChild(folder, Path.GetFileName(filePath));
                var task = _client.PutFile(child.FullPath, stream, new PutFileParameters { CancellationToken = _cancellationToken });
                try
                {
                    do
                    {
                        status.FileProgress = (float)((double)stream.Position / length);
                    } while (!task.IsFaulted && !task.IsCanceled && !task.IsCompleted && !task.Wait(100));
                }
                catch (Exception)
                {
                    throw;
                }
                return child;
            }
        }

        public async Task<SharedOnlineItem> ShareItemAsync(OnlineItem onlineItem)
        {
            // https://doc.owncloud.org/server/7.0/developer_manual/core/ocs-share-api.html
            var keyValuePairs = new Dictionary<string, string>
            {
                { "path", @"/\" + onlineItem.LocalPath.Replace("/", @"\") },
                { "shareType", "3" }
            };

            var response = await PostPropertiesAsync(
                OnlinePathBuilder.FileShareApi(_connectionSettings.StorageUri),
                keyValuePairs
                );
            // {"ocs":{"meta":{"status":"ok","statuscode":100,"message":null},"data":{"id":5,"url":"https:\/\/xxxx.stackstorage.com\/index.php\/s\/abcdef","token":"abcdef"}}}

            dynamic data = response.OCS.Data;
            return new SharedOnlineItem(onlineItem)
            {
                ShareId = (string)data.id,
                ShareUrl = new Uri((string)data.url, UriKind.Absolute)
            };
        }

        public async Task<OwnCloudResponse> SetExpiryDateAsync(SharedOnlineItem onlineItem, DateTime limit)
        {
            var keyValuePairs = new Dictionary<string, string>
            {
                { "expireDate", limit.ToString("yyyy-MM-dd") }
            };

            return await PostPropertiesAsync(OnlinePathBuilder.FileExpirationApi(_connectionSettings.StorageUri, onlineItem), keyValuePairs, "PUT");
            // {"ocs":{"meta":{"status":"ok","statuscode":100,"message":null},"data":[]}}
        }

        public async Task<OwnCloudResponse> SetPasswordAsync(SharedOnlineItem onlineItem, string password)
        {
            var keyValuePairs = new Dictionary<string, string>
            {
                { "password", password }
            };

            return await PostPropertiesAsync(OnlinePathBuilder.FileExpirationApi(_connectionSettings.StorageUri, onlineItem), keyValuePairs, "PUT");
            // {"ocs":{"meta":{"status":"ok","statuscode":100,"message":null},"data":[]}}
        }

        private async Task<OwnCloudResponse> PostPropertiesAsync(Uri uri, IDictionary<string, string> content, string method = "POST")
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.Default.GetBytes($"{_connectionSettings.UserName}:{_connectionSettings.GetPassword()}")));

            var form = new FormUrlEncodedContent(content);

            HttpResponseMessage response;
            if (method == "POST")
                response = await httpClient.PostAsync(uri, form, _cancellationToken);
            else
                response = await httpClient.PutAsync(uri, form, _cancellationToken);

            string data = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<OwnCloudResponse>(data);
        }
    }
}