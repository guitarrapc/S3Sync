using Amazon.S3;
using Amazon.S3.Transfer;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;
using S3Sync.Core;
using S3Sync.Core.LocalFiles;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace S3Sync.BenchmarkCore
{
    public class Program
    {
        static void Main(string[] args)
        {
            var switcher = new BenchmarkSwitcher(new[]
            {
                // Target is Multipart Directory and Files.
                // SinglePart is extremely slow.
                typeof(ConcurrentBenchmark),
            });

            args = new string[] { "0" };
            switcher.Run(args);
        }

        public class BenchmarkConfig : ManualConfig
        {
            public BenchmarkConfig()
            {
                Add(MarkdownExporter.GitHub);
                Add(MemoryDiagnoser.Default);

                // .NETCore
                Add(Job.ShortRun.With(Runtime.Core)
                    .With(CsProjCoreToolchain.NetCoreApp20)
                    .WithWarmupCount(1)
                    .WithTargetCount(1)
                    .WithLaunchCount(1));

                // Full.Net
                Add(Job.ShortRun.With(Runtime.Clr)
                    .With(Jit.RyuJit)
                    .With(Platform.X64)
                    .WithWarmupCount(1)
                    .WithTargetCount(1)
                    .WithLaunchCount(1));
            }
        }

        /// <summary>
        /// Multipart Synchronization
        /// </summary>
        [Config(typeof(BenchmarkConfig))]
        public class ConcurrentBenchmark
        {
            public S3Client S3 { get; set; } = new S3Client(new S3ClientOption { DryRun = true }, AmazonCredential.GetCredential(Environment.GetEnvironmentVariable("S3Sync_Bench_CredentialProfile")));
            public string BucketName { get; set; } = Environment.GetEnvironmentVariable("S3Sync_Bench_BucketName");
            public string LocalRoot { get; set; } = Environment.GetEnvironmentVariable("S3Sync_Bench_LocalRoot");

            [GlobalSetup]
            public void Setup()
            {
            }

            [Benchmark]
            public async Task ConcurrentDirectoryUploadPartsize16CpuX1()
            {
                var directoryUploadRequest = new TransferUtilityUploadDirectoryRequest
                {
                    BucketName = BucketName,
                    Directory = LocalRoot,
                    SearchOption = SearchOption.AllDirectories,
                    SearchPattern = "*",
                    StorageClass = S3StorageClass.ReducedRedundancy,
                };

                //directoryUploadRequest.UploadDirectoryFileRequestEvent += (sender, e) =>
                //{
                //    e.UploadRequest.PartSize = S3.TransferConfig.MinSizeBeforePartUpload;
                //};
                //directoryUploadRequest.UploadDirectoryProgressEvent += (senter, e) =>
                //{
                //    //Console.WriteLine($"{((decimal)e.TransferredBytes / e.TotalBytes).ToString("p")}, {e}");
                //};
                await S3.Transfer.UploadDirectoryAsync(directoryUploadRequest);
            }

            [Benchmark]
            public async Task ConcurrentDirectoryUploadPartsize16CpuX2()
            {
                var directoryUploadRequest = new TransferUtilityUploadDirectoryRequest
                {
                    BucketName = BucketName,
                    Directory = LocalRoot,
                    SearchOption = SearchOption.AllDirectories,
                    SearchPattern = "*",
                    StorageClass = S3StorageClass.ReducedRedundancy,
                };
                //directoryUploadRequest.UploadDirectoryFileRequestEvent += (sender, e) =>
                //{
                //    e.UploadRequest.PartSize = S3.TransferConfig.MinSizeBeforePartUpload;
                //};
                //directoryUploadRequest.UploadDirectoryProgressEvent += (senter, e) =>
                //{
                //    //Console.WriteLine($"{((decimal)e.TransferredBytes / e.TotalBytes).ToString("p")}, {e}");
                //};
                await S3.Transfer2.UploadDirectoryAsync(directoryUploadRequest);
            }

            [Benchmark]
            public async Task ConcurrentDirectoryUploadPartsize5CpuX1()
            {
                var directoryUploadRequest = new TransferUtilityUploadDirectoryRequest
                {
                    BucketName = BucketName,
                    Directory = LocalRoot,
                    SearchOption = SearchOption.AllDirectories,
                    SearchPattern = "*",
                    StorageClass = S3StorageClass.ReducedRedundancy,
                };

                //directoryUploadRequest.UploadDirectoryFileRequestEvent += (sender, e) =>
                //{
                //};
                //directoryUploadRequest.UploadDirectoryProgressEvent += (senter, e) =>
                //{
                //    //Console.WriteLine($"{((decimal)e.TransferredBytes / e.TotalBytes).ToString("p")}, {e}");
                //};
                await S3.Transfer.UploadDirectoryAsync(directoryUploadRequest);
            }

            [Benchmark(Baseline = true)]
            public async Task ConcurretFileUploadPartsize16CpuX1()
            {
                var tasks = new EnumerableFileSystem().EnumerateFiles(LocalRoot)
                .Select(async x =>
                {
                    var fileUploadRequest = new TransferUtilityUploadRequest
                    {
                        BucketName = BucketName,
                        FilePath = x.FullPath,
                        Key = x.FullPath.Replace(LocalRoot + @"\", "").Replace(@"\", "/"),
                        PartSize = S3.TransferConfig.MinSizeBeforePartUpload,
                        StorageClass = S3StorageClass.ReducedRedundancy,
                    };
                    fileUploadRequest.UploadProgressEvent += (sender, e) =>
                    {
                        //Console.WriteLine($"{e.PercentDone}%, {e.FilePath}, {e}");
                    };

                    await S3.Transfer.UploadAsync(fileUploadRequest);
                });
                await Task.WhenAll(tasks);
            }

            [Benchmark]
            public async Task ConcurretFileUploadPartsize16CpuX2()
            {
                var tasks = new EnumerableFileSystem().EnumerateFiles(LocalRoot)
                .Select(async x =>
                {
                    var fileUploadRequest = new TransferUtilityUploadRequest
                    {
                        BucketName = BucketName,
                        FilePath = x.FullPath,
                        Key = x.FullPath.Replace(LocalRoot + @"\", "").Replace(@"\", "/"),
                        PartSize = S3.TransferConfig.MinSizeBeforePartUpload,
                        StorageClass = S3StorageClass.ReducedRedundancy,
                    };
                    await S3.Transfer2.UploadAsync(fileUploadRequest);
                });
                await Task.WhenAll(tasks);
            }

            [Benchmark]
            public async Task ConcurretFileUploadPartsize5CpuX1()
            {
                var tasks = new EnumerableFileSystem().EnumerateFiles(LocalRoot)
                .Select(async x =>
                {
                    var fileUploadRequest = new TransferUtilityUploadRequest
                    {
                        BucketName = BucketName,
                        FilePath = x.FullPath,
                        Key = x.FullPath.Replace(LocalRoot + @"\", "").Replace(@"\", "/"),
                        StorageClass = S3StorageClass.ReducedRedundancy,
                    };
                    await S3.Transfer.UploadAsync(fileUploadRequest);
                });
                await Task.WhenAll(tasks);
            }
        }
    }
}