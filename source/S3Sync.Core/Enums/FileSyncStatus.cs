using System;
using System.Collections.Generic;
using System.Text;

namespace S3Sync.Core
{
    /// <summary>
    /// File Synchronization Status
    /// </summary>
    public enum FileSyncStatus
    {
        Undefined = 0,
        Sync,
        DiffExists,
        LocalOnly,
        RemoteOnly,
    }
}
