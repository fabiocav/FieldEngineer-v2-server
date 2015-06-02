using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FieldEngineerLiteService.DataObjects;
using FieldEngineerLiteService.Files;
using Microsoft.Azure.Mobile.Server.Security;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace FieldEngineerLiteService
{
    public class AzureStorageProvider : StorageProvider
    {
        private readonly static Dictionary<StoragePermissions, SharedAccessBlobPermissions> storagePermissionsMapping;

        private string connectionString;

        static AzureStorageProvider()
        {
            storagePermissionsMapping = new Dictionary<StoragePermissions, SharedAccessBlobPermissions>
            {
                {StoragePermissions.Read, SharedAccessBlobPermissions.Read},
                {StoragePermissions.Write, SharedAccessBlobPermissions.Write},
                {StoragePermissions.Delete, SharedAccessBlobPermissions.Delete},
            };
        }

        public AzureStorageProvider(string connectionString)
        {
            if (connectionString == null)
            {
                throw new ArgumentNullException("connectionString");
            }

            this.connectionString = connectionString;
        }

        public override string Name
        {
            get { return "Microsoft Azure Blob Storage"; }
        }

        public async override Task<IEnumerable<MobileServiceFile>> GetRecordFilesAsync(string tableName, string recordId, IContainerNameResolver containerNameResolver)
        {
            if (tableName == null)
            {
                throw new ArgumentException("tableName");
            }

            if (recordId == null)
            {
                throw new ArgumentException("recordId");
            }

            IEnumerable<string> containerNames = await containerNameResolver.GetRecordContainerNames(tableName, recordId);

            var files = new List<MobileServiceFile>();

            foreach (var containerName in containerNames)
            {
                IEnumerable<CloudBlockBlob> blobs = await GetContainerFilesAsync(containerName);

                files.AddRange(blobs.Select(b => MobileServiceFile.FromBlobItem(b, tableName, recordId)));
            }

            return files;
        }

        public async override Task DeleteFileAsync(string tableName, string recordId, string fileName, IContainerNameResolver containerNameResolver)
        {
            if (tableName == null)
            {
                throw new ArgumentException("tableName");
            }

            if (recordId == null)
            {
                throw new ArgumentException("recordId");
            }

            if (fileName == null)
            {
                throw new ArgumentException("fileName");
            }

            string containerName = await containerNameResolver.GetFileContainerNameAsync(tableName, recordId, fileName);

            CloudBlobContainer container = GetContainer(containerName);

            CloudBlob blob = container.GetBlobReference(fileName);
            await blob.DeleteIfExistsAsync();
        }


        public async override Task<StorageToken> GetAccessTokenAsync(StorageTokenRequest request, StorageTokenScope scope, IContainerNameResolver containerNameResolver)
        {
            string containerName = await containerNameResolver.GetFileContainerNameAsync(request.TargetFile.TableName, request.TargetFile.ParentId, request.TargetFile.Name);

            CloudBlobContainer container = GetContainer(containerName);

            var constraints = new SharedAccessBlobPolicy();
            constraints.SharedAccessExpiryTime = DateTime.UtcNow.AddHours(1);
            constraints.Permissions = GetBlobAccessPermissions(request.Permissions);

            string sas = null;
            string targetUri = null;
            if (scope == StorageTokenScope.File)
            {
                CloudBlockBlob blob = container.GetBlockBlobReference(request.TargetFile.Name);

                targetUri = blob.Uri.ToString();
                sas = await Task.Run(() => blob.GetSharedAccessSignature(constraints));
            }
            else if (scope == StorageTokenScope.Record)
            {
                targetUri = container.Uri.ToString();
                sas = await Task.Run(() => container.GetSharedAccessSignature(constraints));
            }

            var storageToken = new StorageToken();
            storageToken.Permissions = request.Permissions;
            storageToken.RawToken = targetUri + sas;
            storageToken.EntityId = request.TargetFile.ParentId;

            return storageToken;
        }

        protected virtual async Task<IEnumerable<CloudBlockBlob>> GetContainerFilesAsync(string containerName)
        {
            CloudBlobContainer container = GetContainer(containerName);

            IEnumerable<IListBlobItem> blobs = await Task.Run(() => container.ListBlobs(blobListingDetails: BlobListingDetails.Metadata));
            return blobs.OfType<CloudBlockBlob>();
        }

        protected CloudBlobContainer GetContainer(string containerName)
        {
            CloudStorageAccount account = CloudStorageAccount.Parse(this.connectionString);
            CloudBlobClient blobClient = account.CreateCloudBlobClient();

            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            container.CreateIfNotExists();
            return container;
        }

        private SharedAccessBlobPermissions GetBlobAccessPermissions(StoragePermissions storagePermissions)
        {
            SharedAccessBlobPermissions permissions = storagePermissionsMapping
                .Aggregate(SharedAccessBlobPermissions.None, (a, kvp) => (storagePermissions & kvp.Key) == kvp.Key ? a |= kvp.Value : a);

            return permissions;
        }
    }
}
