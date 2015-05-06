using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.OData;
using Microsoft.Azure.Mobile.Server;
using Microsoft.Azure.Mobile.Server.AppService;
using FieldEngineerLiteService.DataObjects;
using FieldEngineerLiteService.Models;
using Salesforce;
using Microsoft.Azure.Mobile.Server.Security;
using Microsoft.Azure.Mobile.Security;
using System;
using Microsoft.Azure.AppService;
using Newtonsoft.Json.Linq;
using System.Configuration;
using System.Net.Http;
using FieldEngineerLiteService.Files;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;
using System.Collections.Generic;

namespace FieldEngineerLiteService.Controllers
{
    public class JobController : TableController<Job>
    {

        private readonly static Dictionary<StoragePermissions, SharedAccessBlobPermissions> storagePermissionsMapping;

        static JobController()
        {
            storagePermissionsMapping = new Dictionary<StoragePermissions, SharedAccessBlobPermissions>
            {
                {StoragePermissions.Read, SharedAccessBlobPermissions.Read},
                {StoragePermissions.Write, SharedAccessBlobPermissions.Write},
                {StoragePermissions.Delete, SharedAccessBlobPermissions.Delete},
            };
        }

        protected override void Initialize(HttpControllerContext controllerContext)
        {
            base.Initialize(controllerContext);
            JobDbContext context = new JobDbContext();
            DomainManager = new EntityDomainManager<Job>(context, Request, Services, enableSoftDelete: true);
        }

        // GET tables/Job
        public IQueryable<Job> GetAllJobs()
        {
            return Query();
        }

        // GET tables/Job/48D68C86-6EA6-4C25-AA33-223FC9A27959
        public SingleResult<Job> GetJob(string id)
        {
            return Lookup(id);
        }

        // PATCH tables/Job/48D68C86-6EA6-4C25-AA33-223FC9A27959
        public async Task<Job> PatchJob(string id, Delta<Job> patch)
        {

            //Job job = patch.GetEntity();  // get new value
            //var user = this.User as ServiceUser;
            //var creds = await user.GetIdentityAsync<AzureActiveDirectoryCredentials>();
            //var token = this.Request.Headers.GetValues("x-zumo-auth").First();
            //SalesforceClient client = new SalesforceClient(false);
            //client.SetUser(user.Id, token);
            //this.Services.Log.Info("Patch: userId: " + user.Id + ", token:" + token);            

            //await client.UpdateCase( "0000" + job.JobNumber, job.Status, job.WorkPerformed);

            return await UpdateAsync(id, patch);
        }

        // POST tables/Job
        public async Task<IHttpActionResult> PostJob(Job item)
        {
            Job current = await InsertAsync(item);
            return CreatedAtRoute("Tables", new { id = current.Id }, current);
        }

        // DELETE tables/Job/48D68C86-6EA6-4C25-AA33-223FC9A27959
        public Task DeleteJob(string id)
        {
            return DeleteAsync(id);
        }

        [HttpPost()]
        [Route("tables/Job/{id}/StorageToken")]
        public async Task<HttpResponseMessage> PostStorageTokenRequest(string id, [FromBody]StorageTokenRequest value)
        {
            ServiceUser user = this.User as ServiceUser;

            if (user == null || !IsTokenRequestValid(value, user))
            {
                this.Unauthorized();
            }

            StorageToken token = await this.GetAccessTokenAsync(value, id, user);

            return Request.CreateResponse(token);
        }

        // Get the files associated with this record
        [HttpGet()]
        [Route("tables/Job/{id}/MobileServiceFiles")]
        public async Task<HttpResponseMessage> GetFiles(string id)
        {
            ServiceUser user = this.User as ServiceUser;

            // Validate user and request

            string containerName = GetContainerNameForRequest(id);
            CloudBlobContainer container = GetRecordContainer(containerName);

            IEnumerable<IListBlobItem> blobs = container.ListBlobs(blobListingDetails: BlobListingDetails.Metadata);
            IEnumerable<MobileServiceFile> files = blobs.OfType<CloudBlockBlob>().Select(b => MobileServiceFile.FromBlobItem(b, "Job", id));
            return Request.CreateResponse(files);
        }

        protected virtual bool IsTokenRequestValid(StorageTokenRequest request, ServiceUser user)
        {
            return true;
        }


        public async Task<StorageToken> GetAccessTokenAsync(StorageTokenRequest request, string entityId, ServiceUser user)
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

    public class MobileServiceFile
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string ParentDataItemType { get; set; }

        public string ParentDataItemId { get; set; }

        public IDictionary<string, string> Metadata { get; set; }

        public static MobileServiceFile FromBlobItem(CloudBlockBlob item, string parentEntityType, string parentEntityId)
        {
            return new MobileServiceFile
            {
                Id = item.Uri.ToString(),
                Name = item.Name,
                ParentDataItemType = parentEntityType,
                ParentDataItemId = parentEntityId,
                Metadata = item.Metadata
            };
        }
    }


}