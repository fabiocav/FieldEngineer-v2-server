using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using System.Text;
using System.Threading.Tasks;
using FieldEngineerLiteService.DataObjects;
using FieldEngineerLiteService.Files;


namespace FieldEngineerLiteService.Controllers
{
    public class JobStorageController : StorageController<Job>
    {
        [HttpPost]
        [Route("tables/Job/{id}/StorageToken")]
        public override Task<HttpResponseMessage> PostStorageTokenRequest(string id, StorageTokenRequest value)
        {
            return base.PostStorageTokenRequest(id, value);
        }

        // Get the files associated with this record
        [HttpGet]
        [Route("tables/Job/{id}/MobileServiceFiles")]
        public override Task<HttpResponseMessage> GetFiles(string id)
        {
            return base.GetFiles(id);
        }

        [HttpDelete]
        [Route("tables/Job/{id}/MobileServiceFiles/{name}")]
        public override Task DeleteFile(string id, string name)
        {
            return base.DeleteFile(id, name);
        }

    }
}
