using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieldEngineerLiteService.Files
{
    [Flags]
    public enum StoragePermissions
    {
        None = 0x0,
        Read = 0x1,
        Write = 0x2,
        Delete = 0x4,
        List = 0x8,
        ReadWrite = Read | Write,
        All = Read | Write | Delete | List
    }
}
