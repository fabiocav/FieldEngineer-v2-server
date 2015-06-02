using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieldEngineerLiteService.Files
{
    public sealed class ContainerNameResolver : IContainerNameResolver
    {
        private string suffix;
        private string prefix;

        public ContainerNameResolver()
        {
        }

        public ContainerNameResolver(string prefix, string suffix)
        {
            this.prefix = prefix;
            this.suffix = suffix;
        }

        public Task<string> GetFileContainerNameAsync(string tableName, string recordId, string fileName)
        {
            return Task.FromResult(GetDefaultContainerName(tableName, recordId));
        }

        public Task<IEnumerable<string>> GetRecordContainerNames(string tableName, string recordId)
        {
            return Task.FromResult<IEnumerable<string>>(new string[] { GetDefaultContainerName(tableName, recordId) });
        }

        private string GetDefaultContainerName(string tableName, string recordId)
        {
            return string.Format("{0}{1}-{2}{3}", prefix, tableName, recordId, suffix).ToLower();
        }
    }
}
