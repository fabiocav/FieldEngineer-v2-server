using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Salesforce.Common;
using Salesforce.Common.Models;
using Salesforce.Force;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;

namespace Salesforce
{
    public class Contact
    {
        public string Name { get; set; }
    }

    public class Case
    {
        public string Id { get; set; }
        public string CaseNumber {get; set;}
        public string Subject {get; set;}
        public Contact Contact {get; set;}
        public string Status {get; set;}
    }

    public static class SalesforceClient
    {
        static public async Task<ForceClient> GetClient()
        {
            var consumerkey = ConfigurationManager.AppSettings["ConsumerKey"];
            var consumersecret = ConfigurationManager.AppSettings["ConsumerSecret"];
            var user = ConfigurationManager.AppSettings["User"];
            var password = ConfigurationManager.AppSettings["Password"];

            var auth = new AuthenticationClient();

            await auth.UsernamePasswordAsync(consumerkey, consumersecret, user, password);
            var client = new ForceClient(auth.InstanceUrl, auth.AccessToken, auth.ApiVersion); 
            Console.WriteLine("Connected to Salesforce"); 
 
            return client;
        }
        
        static public async Task<string> InsertComment(string caseRecordId, string comment)
        {
            ForceClient client = await GetClient();
            dynamic caseComment = new ExpandoObject();
            caseComment.ParentId = caseRecordId;
            caseComment.CommentBody = comment;
            return await client.CreateAsync("CaseComment", caseComment);

        }
        
        static public async Task<string> UpdateCase(string caseNumber, string status, string internalComments)
        {
            status = MapMobileStatus(status);
            var client = await GetClient();

            var recordId = await GetCase(caseNumber);

            if (recordId == null)
                return null;

            string insertResult = await InsertComment(recordId, internalComments);

            dynamic updated = new ExpandoObject();
            updated.Status = status;
            
            var response = await client.UpdateAsync("Case", recordId, updated);
            return response.Success;
        }

        static private async Task<string> GetCase(string caseNumber)
        {
            ForceClient client = await GetClient();

            string query = 
                "SELECT Id, CaseNumber, Subject, Contact.Name, Status FROM Case where CaseNumber = '" + caseNumber + "'";

            var cases = await client.QueryAsync<Case>(query);

            if (cases == null || cases.TotalSize == 0)
                return null;

            return cases.Records.First().Id;
        }

        static public async Task<IEnumerable<Case>> GetActiveCases(string caseNumber)
        {
            string customers = ConfigurationManager.AppSettings["customers"];
            ForceClient client = await GetClient();
            
            string query = "SELECT Id, CaseNumber, Subject, Contact.Name, Status FROM Case where Contact.Name IN (" + customers + ")";
            if (caseNumber != null)
            {
                query += " AND CaseNumber = '" + caseNumber + "'";
            }

            var cases =  await client.QueryAsync<Case>(query);
            
            if (cases == null) return null;
            if (cases.TotalSize == 0) return null;
            return cases.Records;
        }

        static public string MapMobileStatus(string mobileStatus)
        {
            switch (mobileStatus)
            {
                case "Not Started": return "New";
                case "In Progress": return "Working";
                case "Completed": return "Closed";
                default: return "New";
            }
        }
        static public string MapStatus(string salesforceStatus)
        {
            switch (salesforceStatus)
            {
                case "New": return "Not Started";
                case "Working": return "In Progress";
                case "Closed": return "Completed";
                default: return "In Progress";
            }
        }
    }
}