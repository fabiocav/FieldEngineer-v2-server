using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FieldEngineerLiteService.DataObjects;

namespace FieldEngineerLiteService.Files
{
    public class StorageTokenRequest
    {
        public StoragePermissions Permissions { get; set; }

        public string ProviderName { get; set; }

        public MobileServiceFile TargetFile { get; set; }
    }

}
