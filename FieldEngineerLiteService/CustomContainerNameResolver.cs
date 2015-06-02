using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FieldEngineerLiteService.Files;
using Microsoft.WindowsAzure.Storage.Blob;

namespace FieldEngineerLiteService
{
    /// <summary>
    /// An example container name resolver that places special files in a different container.
    /// </summary>
    public class CustomContainerNameResolver : IContainerNameResolver
    {
        private bool userServiceContractContainer;

        public CustomContainerNameResolver(bool useServiceContractContainer)
        {
            this.userServiceContractContainer = useServiceContractContainer;
        }

        public Task<string> GetFileContainerNameAsync(string tableName, string recordId, string fileName)
        {
            string containerName = GetBaseContainerName(tableName, recordId);

            if (userServiceContractContainer)
            {
                containerName += "-sc";
            }
            
            return Task.FromResult(containerName);
        }

        public Task<IEnumerable<string>> GetRecordContainerNames(string tableName, string recordId)
        {
            string baseContainerName = GetBaseContainerName(tableName, recordId);

            return Task.FromResult<IEnumerable<string>>(new[] { baseContainerName, baseContainerName + "-sc" });
        }

        private string GetBaseContainerName(string tableName, string recordId)
        {
            return string.Format("{0}-{1}", tableName, recordId).ToLower();
        }
    }
}
