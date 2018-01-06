using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using S3Sync.Core.Diagnostics;
using S3Sync.Core.Extentions;
using S3Sync.Core.LocalFiles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace S3Sync.Core
{
    public class S3Client : IDisposable
    {
        private static readonly RegionEndpoint defaultEndPoint = RegionEndpoint.APNortheast1;
        private static readonly int queueLength = 10000;

        public AmazonS3Config S3Config { get; private set; }
        public AmazonS3Client Client { get; private set; }
        public TransferUtilityConfig TransferConfig { get; private set; }
        public TransferUtility Transfer { get; private set; }
        public TransferUtility Transfer2 { get; private set; }
        public S3ClientOption Option { get; private set; }

        /// <summary>
        /// IAM Instance Profile Version
        /// </summary>
        public S3Client(S3ClientOption option)
        {
            S3Config = new AmazonS3Config
            {
                RegionEndpoint = defaultEndPoint,
            };
            TransferConfig = new TransferUtilityConfig
            {
                MinSizeBeforePartUpload = 1024 * 1024 * 16, // 16MB
                ConcurrentServiceRequests = Environment.ProcessorCount * 2,
            };
            Client = new AmazonS3Client(S3Config);
            Transfer = new TransferUtility(Client);
            Transfer2 = new TransferUtility(Client, TransferConfig);
            Option = option;
        }

        /// <summary>
        /// AWS Credential Version
        /// </summary>
        /// <param name="credential"></param>
        public S3Client(S3ClientOption option, AWSCredentials credential)
        {
            S3Config = new AmazonS3Config
            {
                RegionEndpoint = defaultEndPoint,
            };
            TransferConfig = new TransferUtilityConfig
            {
                MinSizeBeforePartUpload = 1024 * 1024 * 16, // 16MB
                ConcurrentServiceRequests = Environment.ProcessorCount * 2,
            };
            Client = new AmazonS3Client(credential, S3Config);
            Transfer = new TransferUtility(Client);
            Transfer2 = new TransferUtility(Client, TransferConfig);
            Option = option;
        }

        // Sync

        /// <summary>
        /// Synchronize Local files with S3. (Based on Localfiles.)
        /// </summary>
        /// <param name="localFileInfos"></param>
        /// <param name="bucketName"></param>
        /// <param name="uploadCallback">Sample : e => Console.WriteLine($"{e.PercentDone}%, {e.FilePath}, {e}"</param>
        /// <returns></returns>
        public async Task<SynchronizationResult> SyncWithLocal(SlimFileInfo[] localFileInfos, string bucketName, string prefix, string ignorePrefix, Action<UploadProgressArgs> uploadCallback)
        {
            TimeSpan diffBeforeSyncS3;
            TimeSpan diffBeforeSyncLocal;
            TimeSpan upload = TimeSpan.Zero;
            TimeSpan delete = TimeSpan.Zero;
            TimeSpan total;
            var sw = Stopwatch.StartNew();

            // Exponential backoff preset
            var exponentialBackoff = ExponentialBackoff.Preset.AwsOperation();

            try
            {
                // Obtain files from S3 bucket
                LogTitle("Start : Obtain S3 Items.");
                var s3ObjectList = string.IsNullOrEmpty(prefix)
                    ? await ListAllObjectsAsync(bucketName)
                    : await ListAllObjectsAsync(bucketName, prefix);
                var s3Objects = string.IsNullOrEmpty(ignorePrefix)
                    ? s3ObjectList.ToS3Objects().ToArray()
                    : s3ObjectList.ToS3Objects().IgnorePrefix(ignorePrefix).ToArray();
                diffBeforeSyncS3 = sw.Elapsed;
                Log($"Complete : Obtain S3 Items. {diffBeforeSyncS3.TotalSeconds.ToRound(2)}sec");
                sw.Restart();

                // Obtain current diff
                LogTitle("Start : Calculate Diff.");
                var statuses = GetSysncStatus(localFileInfos, s3Objects, prefix);
                diffBeforeSyncLocal = sw.Elapsed;
                Log($"Complete : Calculate Diff. {diffBeforeSyncLocal.TotalSeconds.ToRound(2)}sec");
                sw.Restart();

                // Diff result
                var skipFiles = statuses.Where(x => x.FileSyncStatus == FileSyncStatus.Sync).ToArray();
                var newFiles = statuses.Where(x => x.FileSyncStatus == FileSyncStatus.LocalOnly).ToArray();
                var updateFiles = statuses.Where(x => x.FileSyncStatus == FileSyncStatus.DiffExists).ToArray();
                var removeFiles = statuses.Where(x => x.FileSyncStatus == FileSyncStatus.RemoteOnly).ToArray();
                var syncResult = new SynchronizationResult()
                {
                    Skip = skipFiles.Length,
                    New = newFiles.Length,
                    Update = updateFiles.Length,
                    Remove = removeFiles.Length,
                    DryRun = Option.DryRun,
                };

                if (Option.DryRun)
                {
                    // Dry run only lease message
                    LogTitle($"Skip : Dryrun is enabled. Skip Synchronize with S3. New = {newFiles.Length}, Update = {updateFiles.Length}, Remove = {removeFiles.Length}");
                }
                else
                {
                    // Upload local files to s3 for diff files
                    LogTitle($"Start : Upload to S3. New = {newFiles.Length}, Update = {updateFiles.Length})");
                    await RetryableFileUploadAsync(bucketName, prefix, uploadCallback, exponentialBackoff, newFiles, updateFiles);
                    upload = sw.Elapsed;
                    Log($"Complete : Upload to S3. {upload.TotalSeconds.ToRound(2)}sec");
                    sw.Restart();

                    // Remove s3 items for diff item
                    LogTitle($"Start : Remove item on S3. Remove = {removeFiles.Length}");
                    if (removeFiles.Any())
                    {
                        await RetrybleFileDeleteAsync(bucketName, exponentialBackoff, removeFiles);
                    }
                    delete = sw.Elapsed;
                    Log($"Complete : Remote item on S3. {delete.TotalSeconds.ToRound(2)}sec");
                }

                // Obtain sync result
                total = diffBeforeSyncS3 + diffBeforeSyncLocal + upload + delete;
                Log($@"
===============================================
Detail Execution Time :
-----------------------------------------------
Obtain S3 Items : {diffBeforeSyncS3.TotalSeconds.ToRound(2)}sec
Calculate Diff  : {diffBeforeSyncLocal.TotalSeconds.ToRound(2)}sec
Upload to S3    : {upload.TotalSeconds.ToRound(2)}sec {(Option.DryRun ? "(dry-run. skipped)" : "")}
Delete on S3    : {delete.TotalSeconds.ToRound(2)}sec {(Option.DryRun ? "(dry-run. skipped)" : "")}
-----------------------------------------------
Total Execution : {total.TotalSeconds.ToRound(2)}sec, ({total.TotalMinutes.ToRound(2)}min)
===============================================");
                return syncResult;
            }
            finally
            {
                sw.Stop();
                sw = null;
            }
        }

        /// <summary>
        /// Upload with controling buffer and auto-retry on error.
        /// </summary>
        /// <remarks>
        /// Due to AWS API limitation, it requires manage upload bandwith and error handling.
        /// Retry tactics will be following 2 patterns:
        /// 1. Continue upload when error happens. (Retry limit : defined retry count = retryLimit)
        ///     - This will be use because it should not stop on error. Retry following process can continue or not.
        /// 2. When (1) successfully complete, include retry, error processing item list will be retry to upload.(Retry limit : defined retry count = retryLimit)
        ///     - If 1 completed then retry errors again will be nice, isn't it?
        /// </remarks>
        /// <param name="bucketName"></param>
        /// <param name="prefix"></param>
        /// <param name="uploadCallback"></param>
        /// <param name="exponentialBackoff"></param>
        /// <param name="newFiles"></param>
        /// <param name="updateFiles"></param>
        /// <returns></returns>
        private async Task RetryableFileUploadAsync(string bucketName, string prefix, Action<UploadProgressArgs> uploadCallback, ExponentialBackoff exponentialBackoff, S3FileHashStatus[] newFiles, S3FileHashStatus[] updateFiles)
        {
            var retryLimit = 5;
            var currentRetry = 0;

            var queueList = new List<Queue<S3FileHashStatus>>();
            var retryQueueList = new List<Queue<S3FileHashStatus>>();

            // Enqueue for upload planned items. This offers more availability when error happens
            foreach (var buffer in newFiles.Concat(updateFiles).Buffer(queueLength))
            {
                var requestQueue = new Queue<S3FileHashStatus>(buffer.Length);
                foreach (var item in buffer)
                {
                    requestQueue.Enqueue(item);
                }
                queueList.Add(requestQueue);
            }

            // Excute for each queue
            var result = await TryRetryableFileUploadAsyncCore(bucketName, prefix, uploadCallback, exponentialBackoff, queueList, retryQueueList);

            // Retry
            while (!result.success && retryQueueList.Any())
            {
                Warn($"Warning: Retrying failed items. {retryQueueList.Sum(x => x.Count)}items");

                // exchange QueueList items
                queueList.Clear();
                foreach (var retryItem in retryQueueList)
                {
                    queueList.Add(retryItem);
                }
                retryQueueList.Clear();

                // execute for each queue
                result = await TryRetryableFileUploadAsyncCore(bucketName, prefix, uploadCallback, exponentialBackoff, queueList, retryQueueList);

                // increment retry count
                currentRetry++;

                // Throw if reached to retry limit.
                if (currentRetry >= retryLimit)
                {
                    Error($"Error : Exceeded retry count limit ({currentRetry}/{retryLimit}). Stop execution.");
                    throw result.exception;
                }
            }
        }

        private async Task<(bool success, AmazonS3Exception exception)> TryRetryableFileUploadAsyncCore(string bucketName, string prefix, Action<UploadProgressArgs> uploadCallback, ExponentialBackoff exponentialBackoff, List<Queue<S3FileHashStatus>> queueList, List<Queue<S3FileHashStatus>> retryQueueList)
        {
            // How many times it retry uploading QueueList.
            var retryLimit = 5;
            var currentRetry = 0;
            AmazonS3Exception exception = null;

            foreach (var queue in queueList)
            {
                try
                {
                    await ConcurretFileUploadAsync(bucketName, queue, prefix, uploadCallback);
                    Log($"Partial Complete : Upload to S3. ({queue.Count})");
                }
                catch (AmazonS3Exception ex)
                {
                    exception = ex;
                    switch (ex.StatusCode)
                    {
                        case HttpStatusCode.ServiceUnavailable:
                            {
                                // Put error queue list into retry queue list.
                                retryQueueList.Add(queue);

                                // re-throw when retry limit exceeded.
                                if (currentRetry >= retryLimit)
                                {
                                    Error($"Error : Exceeded retry count limit ({currentRetry}/{retryLimit}). Stop execution.");
                                    throw ex;
                                }

                                // Request reejected because "Too many Request"? Wait for Exponential Backoff.
                                // Sample Error :
                                // (Status Code : 502) Unhandled Exception: Amazon.S3.AmazonS3Exception: Please reduce your request rate. --->Amazon.Runtime.Internal.HttpErrorResponseException: Exception of type 'Amazon.Runtime.Internal.HttpErrorResponseException' was thrown.
                                var waitTime = exponentialBackoff.GetNextDelay();
                                Warn($"Warning : Exception happen during upload, re-queue to last then wait {waitTime.TotalSeconds}sec for next retry. Exception count in Queue List ({currentRetry}/{retryLimit}). {ex.GetType().FullName}, {ex.Message}, {ex.StackTrace}");

                                // Adjust next retry timing : wait for exponential Backoff
                                await Task.Delay(waitTime);

                                // increment retry count
                                currentRetry++;

                                continue;
                            }
                        default:
                            throw ex;
                    }
                }
            }

            return ((exception == null), exception);
        }

        /// <summary>
        /// Delete with controling buffer and auto-retry on error.
        /// </summary>
        /// <remarks>
        /// Due to AWS API limitation, it requires manage delete bandwith and error handling.
        /// Retry tactics will be following 2 patterns:
        /// 1. Continue delete when error happens. (Retry limit : defined retry count = retryLimit)
        ///     - This will be use because it should not stop on error. Retry following process can continue or not.
        /// 2. When (1) successfully complete, include retry, error processing item list will be retry to delete.(Retry limit : defined retry count = retryLimit)
        ///     - If 1 completed then retry errors again will be nice, isn't it?
        /// </remarks>
        /// <param name="bucketName"></param>
        /// <param name="exponentialBackoff"></param>
        /// <param name="targetFiles"></param>
        /// <returns></returns>
        private async Task RetrybleFileDeleteAsync(string bucketName, ExponentialBackoff exponentialBackoff, S3FileHashStatus[] targetFiles)
        {
            var retryLimit = 5;
            var currentRetry = 0;

            var queueList = new List<Queue<S3FileHashStatus>>();
            var retryQueueList = new List<Queue<S3FileHashStatus>>();

            // Enqueue for remove planned items. This offers more availability when error happens
            foreach (var buffer in targetFiles.Buffer(queueLength))
            {
                var requestQueue = new Queue<S3FileHashStatus>(buffer.Length);
                foreach (var item in buffer)
                {
                    requestQueue.Enqueue(item);
                }
                queueList.Add(requestQueue);
            }

            // execute for each queue
            var result = await TryRetrybleFileDeleteAsyncCore(bucketName, exponentialBackoff, queueList, retryQueueList);

            // Retry
            while (!result.success && retryQueueList.Any())
            {
                Warn($"Warning: Retrying failed items. {retryQueueList.Sum(x => x.Count)}items");

                // exchange QueueList items
                queueList.Clear();
                foreach (var retryItem in retryQueueList)
                {
                    queueList.Add(retryItem);
                }
                retryQueueList.Clear();

                // execute for each queue
                result = await TryRetrybleFileDeleteAsyncCore(bucketName, exponentialBackoff, queueList, retryQueueList);

                // increment retry count
                currentRetry++;

                // Throw if reached to retry limit.
                if (currentRetry >= retryLimit)
                {
                    Error($"Error : Exceeded retry count limit ({currentRetry}/{retryLimit}). Stop execution.");
                    throw result.exception;
                }
            }
        }

        private async Task<(bool success, AmazonS3Exception exception)> TryRetrybleFileDeleteAsyncCore(string bucketName, ExponentialBackoff exponentialBackoff, List<Queue<S3FileHashStatus>> queueList, List<Queue<S3FileHashStatus>> retryQueueList)
        {
            // How many times it retry uploading QueueList.
            var retryLimit = 5;
            var currentRetry = 0;
            AmazonS3Exception exception = null;

            foreach (var queue in queueList)
            {
                try
                {
                    await ConcurrentFileDeleteAsync(bucketName, queue);
                    Log($"Partial Complete : Delete to S3. ({queue.Count})");
                }
                catch (AmazonS3Exception ex)
                {
                    exception = ex;
                    switch (ex.StatusCode)
                    {
                        case HttpStatusCode.ServiceUnavailable:
                            {
                                // Put error queue list into retry queue list.
                                retryQueueList.Add(queue);

                                // re-throw when retry limit exceeded.
                                if (currentRetry >= retryLimit)
                                {
                                    Error($"Error : Exceeded retry count limit ({currentRetry}/{retryLimit}). Stop execution.");
                                    throw ex;
                                }

                                // Request reejected because "Too many Request"? Wait for Exponential Backoff.
                                // Sample : (Status Code : 502) Unhandled Exception: Amazon.S3.AmazonS3Exception: Please reduce your request rate. --->Amazon.Runtime.Internal.HttpErrorResponseException: Exception of type 'Amazon.Runtime.Internal.HttpErrorResponseException' was thrown.
                                var waitTime = exponentialBackoff.GetNextDelay();
                                Warn($"Warning : Exception happen during delete, re-queue to last then wait {waitTime.TotalSeconds}sec for next retry. Exception count in Queue List ({currentRetry}/{retryLimit}). {ex.GetType().FullName}, {ex.Message}, {ex.StackTrace}");

                                // Adjust next retry timing : wait for exponential Backoff
                                await Task.Delay(waitTime);

                                // increment retry count
                                currentRetry++;

                                continue;
                            }
                        default:
                            throw ex;
                    }
                }
            }

            return ((exception == null), exception);
        }

        // List

        public async Task<List<ListObjectsV2Response>> ListAllObjectsAsync(string bucketName)
        {
            var list = new List<ListObjectsV2Response>();
            var res = await Client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = bucketName,
                MaxKeys = int.MaxValue,
            });
            if (res != null)
            {
                list.Add(res);
            }

            while (!string.IsNullOrEmpty(res.NextContinuationToken))
            {
                res = await Client.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName = bucketName,
                    MaxKeys = int.MaxValue,
                    ContinuationToken = res.NextContinuationToken,
                });
                if (res != null)
                {
                    list.Add(res);
                }
            }

            return list;
        }

        public async Task<List<ListObjectsV2Response>> ListAllObjectsAsync(string bucketName, string prefix)
        {
            var list = new List<ListObjectsV2Response>();
            var res = await Client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = bucketName,
                MaxKeys = int.MaxValue,
                Prefix = prefix,
            });
            if (res != null)
            {
                list.Add(res);
            }

            while (!string.IsNullOrEmpty(res.NextContinuationToken))
            {
                res = await Client.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName = bucketName,
                    MaxKeys = int.MaxValue,
                    Prefix = prefix,
                    ContinuationToken = res.NextContinuationToken,
                });
                if (res != null)
                {
                    list.Add(res);
                }
            }

            return list;
        }

        public async Task<List<ListObjectsV2Response>> ListAllObjectsAsync(ListObjectsV2Request request)
        {
            var list = new List<ListObjectsV2Response>();
            var res = await ListObjectsAsync(request);
            if (res != null)
            {
                list.Add(res);
            }

            while (!string.IsNullOrEmpty(res.NextContinuationToken))
            {
                res = await ListObjectsAsync(request, res.NextContinuationToken);
                if (res != null)
                {
                    list.Add(res);
                }
            }

            return list;
        }

        public async Task<ListObjectsV2Response> ListObjectsAsync(ListObjectsV2Request request)
        {
            var res = await Client.ListObjectsV2Async(request);
            return res;
        }
        public async Task<ListObjectsV2Response> ListObjectsAsync(ListObjectsV2Request request, string continuationToken)
        {
            request.ContinuationToken = continuationToken;
            var res = await Client.ListObjectsV2Async(request);
            return res;
        }

        // Get

        public async Task<GetObjectResponse> GetObjectsAsync(string bucketName, string keyName)
        {
            var res = await Client.GetObjectAsync(bucketName, keyName);
            return res;
        }

        public async Task<GetObjectMetadataResponse> GetMetaAsync(string bucketName, string keyName)
        {
            var res = await Client.GetObjectMetadataAsync(bucketName, keyName);
            return res;
        }

        // Upload

        public async Task ConcurretFileUploadAsync(string bucketName, IEnumerable<S3FileHashStatus> targetFiles, Action<UploadProgressArgs> uploadProgressEventAction)
        {
            var tasks = targetFiles.Select(async x =>
            {
                var request = new TransferUtilityUploadRequest
                {
                    BucketName = bucketName,
                    FilePath = x.FileInfo.Value.FullPath,
                    Key = x.FileInfo.Value.MultiplatformRelativePath,
                    PartSize = TransferConfig.MinSizeBeforePartUpload,
                    StorageClass = S3StorageClass.Standard,
                };
                if (!string.IsNullOrEmpty(Option.ContentType))
                {
                    request.ContentType = Option.ContentType;
                }

                if (uploadProgressEventAction != null)
                {
                    request.UploadProgressEvent += (sender, e) =>
                    {
                        uploadProgressEventAction(e);
                    };
                }

                await Transfer.UploadAsync(request);
            });
            await Task.WhenAll(tasks);
        }

        public async Task ConcurretFileUploadAsync(string bucketName, IEnumerable<S3FileHashStatus> targetFiles, string prefix, Action<UploadProgressArgs> uploadProgressEventAction)
        {
            var tasks = targetFiles.Select(async x =>
            {
                var request = new TransferUtilityUploadRequest
                {
                    BucketName = bucketName,
                    FilePath = x.FileInfo.Value.FullPath,
                    Key = GetS3Key(prefix, x.FileInfo.Value.MultiplatformRelativePath),
                    PartSize = TransferConfig.MinSizeBeforePartUpload,
                    StorageClass = S3StorageClass.Standard,
                };
                if (!string.IsNullOrEmpty(Option.ContentType))
                {
                    request.ContentType = Option.ContentType;
                }

                if (uploadProgressEventAction != null)
                {
                    request.UploadProgressEvent += (sender, e) =>
                    {
                        uploadProgressEventAction(e);
                    };
                }

                await Transfer.UploadAsync(request);
            });
            await Task.WhenAll(tasks);
        }

        // Delete

        public async Task ConcurrentFileDeleteAsync(string bucketName, IEnumerable<S3FileHashStatus> targetFiles)
        {
            var tasks = targetFiles.Select(async x =>
            {
                var request = new DeleteObjectRequest
                {
                    BucketName = bucketName,
                    Key = x.S3Object.Key,
                };

                await Client.DeleteObjectAsync(request);
            });
            await Task.WhenAll(tasks);
        }

        public async Task ConcurrentFileDeleteAsync(string bucketName, string prefix, IEnumerable<S3FileHashStatus> targetFiles)
        {
            var tasks = targetFiles.Select(async x =>
            {
                var request = new DeleteObjectRequest
                {
                    BucketName = bucketName,
                    Key = GetS3Key(prefix, x.S3Object.Key),
                };
                await Client.DeleteObjectAsync(request);
            });
            await Task.WhenAll(tasks);
        }

        // Diff

        public S3FileHashStatus[] GetSysncStatus(IEnumerable<SlimFileInfo> localFiles, IEnumerable<S3Object> s3Objects, string prefix)
        {
            if (localFiles == null) throw new ArgumentNullException(nameof(localFiles));
            if (s3Objects == null) throw new ArgumentNullException(nameof(s3Objects));

            // Dictionary for Remote S3 and Local File
            var s3Dictionary = s3Objects.ToDictionary(x => x.Key, x => x);
            var localDictionary = localFiles.ToDictionary(x => GetS3Key(prefix, x.MultiplatformRelativePath), x => x);

            // Get State for Local files
            S3FileHashStatus[] statuses = null;
            var localExists = localDictionary.Select(x =>
            {
                int chunkSize = 0;
                string fileETag = "";
                if (s3Dictionary.TryGetValue(x.Key, out S3Object s3Object))
                {
                    var bytes = FileHashHelper.GetFileBinary(x.Value.FullPath);
                    var s3ETagChunkCount = s3Object.GetETagChunkCount();
                    chunkSize = FileHashHelper.GetChunkSize(bytes.Length, s3ETagChunkCount);
                    fileETag = FileHashHelper.CalculateEtag(bytes, chunkSize);
                }

                return new S3FileHashStatus(x.Value, fileETag, chunkSize, s3Object);
            })
            .ToArray();

            // Get State for Remote S3
            var remoteOnly = s3Objects
                .Where(x => !localDictionary.TryGetValue(x.Key, out var slimFileInfo))
                .Select(x => new S3FileHashStatus(null, "", 0, x))
                .ToArray();

            // Concat local and remote
            statuses = localExists.Concat(remoteOnly).ToArray();

            return statuses;
        }

        public void Dispose()
        {
            Client?.Dispose();
            Transfer?.Dispose();
        }

        // Helper

        private static string GetS3Key(string keyPrefix, string key)
        {
            return string.IsNullOrEmpty(keyPrefix) ? key : $"{keyPrefix}/{key}";
        }

        // Logger

        public static void Error(string text)
        {
            Console.Error.WriteLine(text);
        }

        public static void Warn(string text)
        {
            Log(text, ConsoleColor.DarkYellow);
        }

        private static void LogTitle(string text)
        {
            Log($@"
-----------------------------------------------
{text}
-----------------------------------------------", ConsoleColor.White);
        }

        public static void Log(string text)
        {
            Log(text, ConsoleColor.DarkGray);
        }

        private static void Log(string text, ConsoleColor color)
        {
            lock (typeof(S3Client))
            {
                var oldColor = Console.ForegroundColor;
                if (oldColor != color)
                {
                    Console.ForegroundColor = color;
                }

                Console.WriteLine(text);

                if (oldColor != color)
                {
                    Console.ForegroundColor = oldColor;
                }
            }
        }
    }
}
