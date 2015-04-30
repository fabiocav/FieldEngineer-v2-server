using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.OData;
using Microsoft.Azure.Mobile.Server;
using FieldEngineerLiteService.DataObjects;
using FieldEngineerLiteService.Models;
using Salesforce;
using System.Net;
using System.Net.Http;

namespace FieldEngineerLiteService.Controllers
{
    public class JobController : TableController<Job>
    {
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
            try 
            {
                var result = await UpdateAsync(id, patch);
                await SalesforceClient.UpdateCase("0000" + result.JobNumber, result.Status, result.WorkPerformed);

                return result;
            }

            catch (HttpResponseException e)
            {
                if (e.Response != null && e.Response.StatusCode == HttpStatusCode.PreconditionFailed)
                {
                    // Handle conflict
                    var content = e.Response.Content as ObjectContent;
                    Job serverItem = (Job)content.Value;

                    Services.Log.Info("Server wins" + serverItem.JobNumber);
                    return serverItem;
                }
                else
                {
                    throw e;
                }
            }
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
    }
}