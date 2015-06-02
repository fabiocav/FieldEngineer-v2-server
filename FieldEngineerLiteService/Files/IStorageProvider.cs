using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FieldEngineerLiteService.DataObjects;
using FieldEngineerLiteService.Files;
using Microsoft.Azure.Mobile.Server.Security;

namespace FieldEngineerLiteService
{
    public abstract class StorageProvider
    {
        abstract public string Name { get; }

        abstract public Task<IEnumerable<MobileServiceFile>> GetRecordFilesAsync(string tableName, string recordId, IContainerNameResolver containerNameResolver);

        abstract public Task DeleteFileAsync(string tableName, string recordId, string fileName, IContainerNameResolver containerNameResolver);

        abstract public Task<StorageToken> GetAccessTokenAsync(StorageTokenRequest request, StorageTokenScope scope, IContainerNameResolver containerNameResolver);
    }
}
