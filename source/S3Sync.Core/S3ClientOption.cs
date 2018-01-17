using System;
using System.Collections.Generic;
using System.Text;

namespace S3Sync.Core
{
    public class S3ClientOption
    {
        public bool DryRun { get; set; } = true;
        public string ContentType { get; set; }
        public string Region { get; set; }
    }
}
