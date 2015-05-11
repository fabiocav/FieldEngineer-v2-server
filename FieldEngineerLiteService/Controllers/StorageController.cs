using FieldEngineerLiteService.DataObjects;
using FieldEngineerLiteService.Files;
using Microsoft.Azure.Mobile.Server;
using Microsoft.Azure.Mobile.Server.Security;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace FieldEngineerLiteService.Controllers
{
    public abstract class StorageController<T> : ApiController
    {

        private readonly static Dictionary<StoragePermissions, SharedAccessBlobPermissions> storagePermissionsMapping;

        static StorageController()
        {
            storagePermissionsMapping = new Dictionary<StoragePermissions, SharedAccessBlobPermissions>
            {
                {StoragePermissions.Read, SharedAccessBlobPermissions.Read},
                {StoragePermissions.Write, SharedAccessBlobPermissions.Write},
                {StoragePermissions.Delete, SharedAccessBlobPermissions.Delete},
            };
        }

        public ApiServices Services { get; set; }

        public virtual async Task<HttpResponseMessage> PostStorageTokenRequest(string id, [FromBody]StorageTokenRequest value)
        {
            ServiceUser user = this.User as ServiceUser;

            if (user == null || !IsTokenRequestValid(value, user))
            {
                this.Unauthorized();
            }

            StorageToken token = await GetAccessTokenAsync(value, id, user);

            return Request.CreateResponse(token);
        }

        public virtual async Task<HttpResponseMessage> GetFiles(string id)
        {
            ServiceUser user = this.User as ServiceUser;

            // Validate user and request

            string containerName = GetContainerNameForRequest(id);
            CloudBlobContainer container = GetRecordContainer(containerName);

            IEnumerable<IListBlobItem> blobs = await Task.Run(() => container.ListBlobs(blobListingDetails: BlobListingDetails.Metadata));
            IEnumerable<MobileServiceFile> files = blobs.OfType<CloudBlockBlob>().Select(b => MobileServiceFile.FromBlobItem(b, "Job", id));
            return Request.CreateResponse(files);
        }

        public virtual async Task DeleteFile(string id, string name)
        {
            ServiceUser user = this.User as ServiceUser;

            // Validate user and request

            string containerName = GetContainerNameForRequest(id);
            CloudBlobContainer container = GetRecordContainer(containerName);

            CloudBlob blob = container.GetBlobReference(name);
            await blob.DeleteIfExistsAsync();
        }

        protected virtual bool IsTokenRequestValid(StorageTokenRequest request, ServiceUser user)
        {
            return true;
        }


        protected async Task<StorageToken> GetAccessTokenAsync(StorageTokenRequest request, string entityId, ServiceUser user)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            string containerName = this.GetContainerNameForRequest(entityId);
            CloudBlobContainer container = GetRecordContainer(containerName);

            var constraints = new SharedAccessBlobPolicy();
            constraints.SharedAccessExpiryTime = DateTime.UtcNow.AddHours(1);
            constraints.Permissions = GetBlobAccessPermissions(request.Permissions);

            string containerToken = await Task.Run(() => container.GetSharedAccessSignature(constraints));

            var storageToken = new StorageToken();
            storageToken.Permissions = request.Permissions;
            storageToken.RawToken = container.Uri + containerToken;
            storageToken.EntityId = entityId;

            return storageToken;
        }

        private CloudBlobContainer GetRecordContainer(string containerName)
        {
            CloudStorageAccount account = CloudStorageAccount.Parse(this.Services.Settings.Connections["mS_AzureStorageAccountConnectionString"].ConnectionString);
            CloudBlobClient blobClient = account.CreateCloudBlobClient();

            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            container.CreateIfNotExists();
            return container;
        }

        private string GetContainerNameForRequest(string entityId)
        {
            // - Default format: entityname-recordid
            // - Expose a mechanism to allow developers to change how names are resolved.
            // - Need validation to ensure we have a valid container name: https://msdn.microsoft.com/en-us/library/azure/dd135715.aspx

            return string.Format("job-{0}", entityId);
        }

        private SharedAccessBlobPermissions GetBlobAccessPermissions(StoragePermissions storagePermissions)
        {
            SharedAccessBlobPermissions permissions = storagePermissionsMapping
                .Aggregate(SharedAccessBlobPermissions.None, (a, kvp) => (storagePermissions & kvp.Key) == kvp.Key ? a |= kvp.Value : a);

            return permissions;
        }
    }
}
