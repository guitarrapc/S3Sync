using Amazon.S3.Model;
using S3Sync.Core.LocalFiles;

namespace S3Sync.Core
{
    public struct S3FileHashStatus
    {
        public FileSyncStatus FileSyncStatus { get; }
        public bool IsFileMatchS3ETag { get; }
        public SlimFileInfo? FileInfo { get; }
        public string FileHashHexString { get; }
        public int ChunkSize { get; }
        public S3Object S3Object { get; }
        public string S3Etag { get; }

        public S3FileHashStatus(SlimFileInfo? fileInfo, string fileHashHexString, int chunkSize, S3Object s3Object)
        {
            FileInfo = fileInfo;
            FileHashHexString = fileHashHexString;
            ChunkSize = chunkSize;
            S3Object = s3Object;
            S3Etag = S3Object?.GetETag();
            IsFileMatchS3ETag = FileHashHexString == S3Etag;

            // Local : Not exists.
            // S3 : Exists
            if (!FileInfo.HasValue && S3Object != null)
            {
                FileSyncStatus = FileSyncStatus.RemoteOnly;
            }
            // Local : Exsits
            // S3 : Not exsits
            else if (FileInfo.HasValue && S3Object == null)
            {
                FileSyncStatus = FileSyncStatus.LocalOnly;
            }
            // Unmatch Calculated Etag and S3Object ETag.
            // Possible : Rewritten on Remote OR rewriteten on Local 
            else if (!IsFileMatchS3ETag)
            {
                FileSyncStatus = FileSyncStatus.DiffExists;
            }
            // Match Calculated Etag and S3Object ETag.
            else if (IsFileMatchS3ETag)
            {
                FileSyncStatus = FileSyncStatus.Sync;
            }
            // Status is invalid. Not expected at all. (May be new AWS Implementation?)
            else
            {
                FileSyncStatus = FileSyncStatus.Undefined;
            }
        }
    }
}
