using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using System.Text;
using System.Threading.Tasks;
using FieldEngineerLiteService.DataObjects;
using FieldEngineerLiteService.Files;
using System.Text.RegularExpressions;


namespace FieldEngineerLiteService.Controllers
{
    public class JobStorageController : StorageController<Job>
    {
        [HttpPost]
        [Route("tables/Job/{id}/StorageToken")]
        public async Task<HttpResponseMessage> PostStorageTokenRequest(string id, StorageTokenRequest value)
        {
            // For this example, we're trusting the metadata provided by the client... 
            // In this step, we'd validate the credentials and the metadata before requesting the SAS
            bool isServiceContractRequest = value.TargetFile.Metadata != null &&
                value.TargetFile.Metadata.ContainsKey("isServiceContract") &&
                string.Compare(value.TargetFile.Metadata["isServiceContract"], "true", true) == 0;

            StorageToken token = await GetStorageTokenAsync(id, value, new CustomContainerNameResolver(isServiceContractRequest));

            return Request.CreateResponse(token);
        }

        // Get the files associated with this record
        [HttpGet]
        [Route("tables/Job/{id}/MobileServiceFiles")]
        public async Task<HttpResponseMessage> GetFiles(string id)
        {
            IEnumerable<MobileServiceFile> files = await GetRecordFilesAsync(id, new CustomContainerNameResolver(true));

            return Request.CreateResponse(files);
        }

        [HttpDelete]
        [Route("tables/Job/{id}/MobileServiceFiles/{name}")]
        public Task Delete(string id, string name)
        {
            bool isServiceContractRequest = IsServiceContractRequest(Request);

            return base.DeleteFileAsync(id, name, new CustomContainerNameResolver(isServiceContractRequest));
        }

        private bool IsServiceContractRequest(HttpRequestMessage request)
        {
            var queryString = Request.GetQueryNameValuePairs()
                .Where(q => string.Compare(q.Key, "x-zumo-filestoreuri", true) == 0)
                .FirstOrDefault();

            return Regex.IsMatch(queryString.Value ?? string.Empty, "^\\/?.*-sc\\/");
        }

    }
}
