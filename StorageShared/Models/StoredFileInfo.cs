using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageShared.Models
{
    public class StoredFileInfo
    {
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public long Size { get; set; }
        public string Hash { get; set; }

        public StoredFileInfo(string name, DateTime createdAt, long size, string hash)
        {
            Name = name;
            CreatedAt = createdAt;
            Size = size;
            Hash = hash;
        }
    }
}
