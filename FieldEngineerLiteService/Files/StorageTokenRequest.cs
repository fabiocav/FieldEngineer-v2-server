using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieldEngineerLiteService.Files
{
    public class StorageTokenRequest
    {
        public StoragePermissions Permissions { get; set; }

        public string ProviderName { get; set; }
    }

}
