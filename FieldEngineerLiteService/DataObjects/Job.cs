using System;
using System.Collections.Generic;
using Microsoft.Azure.Mobile.Server;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

namespace FieldEngineerLiteService.DataObjects
{
    public class Job : EntityData
    {
        public string AgentId { get; set; }

        public string JobNumber { get; set; }

        public string Title { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string Status { get; set; }

        public string CustomerName { get; set; }
        public string CustomerAddress { get; set; }
        public string CustomerPhoneNumber { get; set; }

        public string WorkPerformed { get; set; }

        //public MobileServiceFile Image { get; set; }
    }


    public class MobileServiceFile
    {
        private readonly static Lazy<MobileServiceFile> _emptyFile;

        static MobileServiceFile()
        {
            _emptyFile = new Lazy<MobileServiceFile>(() => new NullMobileServiceFile());
        }

        public virtual string Id { get; set; }

        public virtual string Name { get; set; }

        public virtual string TableName { get; set; }

        public virtual string ParentId { get; set; }

        public virtual string ContentMD5 { get; set; }

        public virtual long Length { get; set; }

        public DateTimeOffset? LastModified { get; set; }

        public string StoreUri { get; set; }

        public virtual IDictionary<string, string> Metadata { get; set; }

        public static MobileServiceFile FromBlobItem(CloudBlockBlob item, string parentEntityType, string parentEntityId)
        {
            return new MobileServiceFile
            {
                Id = item.Name,
                Name = item.Name,
                TableName = parentEntityType,
                ParentId = parentEntityId,
                Length = item.Properties.Length,
                ContentMD5 = item.Properties.ContentMD5,
                LastModified = item.Properties.LastModified,
                Metadata = item.Metadata,
                StoreUri = item.Uri.LocalPath
            };
        }

        [JsonIgnore]
        public virtual string FileInfoToken
        {
            get
            {
                return JsonConvert.SerializeObject(this);
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    return;
                }
                try
                {
                    JsonConvert.PopulateObject(value, this,
                        new JsonSerializerSettings
                        {
                            Error = (a, s) => s.ErrorContext.Handled = true // WTH!
                        });
                }
                catch { }
            }
        }

        //public MobileServiceFile Empty
        //{
        //    get
        //    {
        //        return _emptyFile.Value;
        //    }
        //}

        private class NullMobileServiceFile : MobileServiceFile
        {
            private const string CannotModifyInstanceExceptionMessage = "Cannot modify this MobileServiceFile instance";

            public override string TableName
            {
                get
                {
                    return null;
                }
                set { throw new InvalidOperationException(CannotModifyInstanceExceptionMessage); }
            }

            public override string ContentMD5
            {
                get
                {
                    return null;
                }
                set { throw new InvalidOperationException(CannotModifyInstanceExceptionMessage); }
            }

            public override string FileInfoToken
            {
                get
                {
                    return null;
                }
                set { throw new InvalidOperationException(CannotModifyInstanceExceptionMessage); }
            }

            public override string Id
            {
                get
                {
                    return null;
                }
                set { throw new InvalidOperationException(CannotModifyInstanceExceptionMessage); }
            }

            public override long Length
            {
                get
                {
                    return 0;
                }
                set { throw new InvalidOperationException(CannotModifyInstanceExceptionMessage); }
            }

            public override IDictionary<string, string> Metadata
            {
                get
                {
                    return null;
                }
                set { throw new InvalidOperationException(CannotModifyInstanceExceptionMessage); }
            }

            public override string Name
            {
                get
                {
                    return null;
                }
                set { throw new InvalidOperationException(CannotModifyInstanceExceptionMessage); }
            }
        }
    }


}