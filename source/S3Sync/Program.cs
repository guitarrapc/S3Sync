using System;
using S3Sync.Core;
using S3Sync.Core.LocalFiles;
using System.Linq;
using Amazon.S3.Transfer;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using S3Sync.Core.Diagnostics;

namespace S3Sync
{
    class Program
    {
        private static string BucketName { get; set; }
        private static string LocalRoot { get; set; }
        private static string KeyPrefix { get; set; }
        private static string IgnoreKeyPrefix { get; set; }
        private static string[] ExcludeFiles { get; set; }
        private static string[] ExcludeDirectories { get; set; }
        private static bool Silent { get; set; }
        private static string CredentialProfile { get; set; }
        private static Action<UploadProgressArgs> UploadCallback { get; set; }

        private enum ArgumentType
        {
            BucketName = 0,
            LocalRoot,
            KeyPrefix,
            IgnoreKeyPrefix,
            ExcludeFiles,
            ExcludeDirectories,
            Silent,
            CredentialProfile,
        }

        private enum EnvType
        {
            S3Sync_BucketName = 0,
            S3Sync_LocalRoot,
            S3Sync_KeyPrefix,
            S3Sync_IgnoreKeyPrefix,
            S3Sync_ExcludeFiles,
            S3Sync_ExcludeDirectories,
            S3Sync_Silent,
            S3Sync_CredentialProfile,
        }

        /// <summary>
        /// Sample .NETCore : dotnet S3Sync.dll BucketName=guitarrapc-multipart-test LocalRoot=C:\HogeMogeImages ExcludeFiles=.gitignore,.gitattributes ExcludeDirectories=.git,test
        /// Sample Full.NET : S3Sync.exe BucketName=guitarrapc-multipart-test KeyPrefix=hoge LocalRoot=C:\HomeMogeImages ExcludeFiles=.gitignore,.gitattributes, ExcludeDirectories=.git,test
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            try
            {
                MainCoreAsync(args).GetAwaiter().GetResult();
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                {
                    Console.Error.WriteLine($"{ex.InnerException.Message}, {ex.InnerException.GetType().FullName}, {ex.InnerException.StackTrace}");
                }
                Console.Error.WriteLine($"{ex.Message}, {ex.GetType().FullName}, {ex.StackTrace}");
                Environment.Exit(1);
            }
        }

        static async Task MainCoreAsync(string[] args)
        {
            var sw = Stopwatch.StartNew();

            // Apply initial settings
            ApplyInitialConfiguration();

            // Validate argumanges
            LogTitle("Start : Evaluate Arguments. Override with EnvironmentVariables if missing argument.");
            EvaluateArguments(args);

            // Set UploadCallback
            if (!Silent)
            {
                // アップロード時のコールバック登録
                UploadCallback = e => Log($"{e.PercentDone}%, {e.FilePath}, {nameof(KeyPrefix)} : {KeyPrefix}, {e}");
            }

            // Obtain local files
            LogTitle("Start : Obtain Local items.");
            var localFileInfos = new EnumerableFileSystem(ExcludeDirectories, ExcludeFiles)
                .EnumerateFiles(LocalRoot)
                .ToArray();

            var obtainLocal = sw.Elapsed;
            Log($@"Complete : Obtain Local items.", obtainLocal);
            sw.Restart();

            // Get Credential
            // Missing CredentialProfile : Use IAM Instance Profile
            // Found CredentialProfile : Use as ProfileName
            LogTitle("Start : Obtain credential");
            var s3 = string.IsNullOrEmpty(CredentialProfile)
                ? new S3Client()
                : new S3Client(AmazonCredential.GetCredential(CredentialProfile));

            // Begin Synchronization
            LogTitle("Start : Synchronization");
            var result = await s3.SyncWithLocal(localFileInfos, BucketName, KeyPrefix, IgnoreKeyPrefix, UploadCallback);

            // Show result
            LogTitle($@"Show : Synchronization result as follows.");
            Warn(result.ToMarkdown());
            var synchronization = sw.Elapsed;
            Log($@"Complete : Synchronization.", synchronization);

            // Total result
            Log($@"Total :", (obtainLocal + synchronization));
        }

        static void ApplyInitialConfiguration()
        {
            // Web周りの設定
            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.DefaultConnectionLimit = 96;
        }

        static void EvaluateArguments(string[] args)
        {
            // BucketName=nantokakantokabucket
            BucketName = args.Where(x => x.StartsWith(ArgumentType.BucketName.ToString(), StringComparison.InvariantCultureIgnoreCase))
                .SelectMany(x => x.SplitEx("="))
                .LastOrDefault()
                ?? GetEnvValueString(ArgumentType.BucketName, EnvType.S3Sync_BucketName);

            // LocalRoot=c:\hogemoge
            LocalRoot = args.Where(x => x.StartsWith(ArgumentType.LocalRoot.ToString(), StringComparison.InvariantCultureIgnoreCase))
                .SelectMany(x => x.SplitEx("="))
                .LastOrDefault()
                ?? GetEnvValueString(ArgumentType.LocalRoot, EnvType.S3Sync_LocalRoot);

            // KeyPrefix=image
            KeyPrefix = args.Where(x => x.StartsWith(ArgumentType.KeyPrefix.ToString(), StringComparison.InvariantCultureIgnoreCase))
                .SelectMany(x => x.SplitEx("="))
                .LastOrDefault()
                ?.TrimEnd('/')
                ?? GetEnvValueString(ArgumentType.KeyPrefix, EnvType.S3Sync_KeyPrefix);

            // IgnoreKeyPrefix=image
            IgnoreKeyPrefix = args.Where(x => x.StartsWith(ArgumentType.IgnoreKeyPrefix.ToString(), StringComparison.InvariantCultureIgnoreCase))
                .SelectMany(x => x.SplitEx("="))
                .LastOrDefault()
                ?.TrimEnd('/')
                ?? GetEnvValueString(ArgumentType.IgnoreKeyPrefix, EnvType.S3Sync_IgnoreKeyPrefix);

            // ExcludeFiles=hogemoge,fugafuga
            ExcludeFiles = args.Where(x => x.StartsWith(ArgumentType.ExcludeFiles.ToString(), StringComparison.InvariantCultureIgnoreCase))
                .SelectMany(x => x.SplitEx("="))
                .LastOrDefault()
                ?.SplitEx(",")
                .Select(x => x.Trim())
                .ToArray()
                ?? GetEnvValueString(ArgumentType.ExcludeFiles, EnvType.S3Sync_ExcludeFiles)
                ?.SplitEx(",");

            // ExcludeDirectories=hogemoge,fugafuga
            ExcludeDirectories = args.Where(x => x.StartsWith(ArgumentType.ExcludeDirectories.ToString(), StringComparison.InvariantCultureIgnoreCase))
                .SelectMany(x => x.SplitEx("="))
                .LastOrDefault()
                ?.SplitEx(",")
                ?.Select(x => x.Trim())
                .ToArray()
                ?? GetEnvValueString(ArgumentType.ExcludeDirectories, EnvType.S3Sync_ExcludeDirectories)
                ?.SplitEx(",");

            // Silent=false
            Silent = bool.Parse(args.Where(x => x.StartsWith(ArgumentType.Silent.ToString(), StringComparison.InvariantCultureIgnoreCase))
                .SelectMany(x => x.SplitEx("="))
                .Where(x => string.Equals(x, "true", StringComparison.InvariantCultureIgnoreCase) || string.Equals(x, "false", StringComparison.InvariantCultureIgnoreCase))
                .LastOrDefault()
                ?.Trim()
                ?? GetEnvValueString(ArgumentType.Silent, EnvType.S3Sync_Silent)
                    ?? "false");

            // CredentialProfile=ProfileName
            CredentialProfile = args.Where(x => x.StartsWith(ArgumentType.CredentialProfile.ToString(), StringComparison.InvariantCultureIgnoreCase))
                .SelectMany(x => x.SplitEx("="))
                .LastOrDefault()
                ?.Trim()
                ?? GetEnvValueString(ArgumentType.CredentialProfile, EnvType.S3Sync_CredentialProfile);

            // Show Arguments
            Log($"{nameof(BucketName)} : {BucketName}");
            Log($"{nameof(LocalRoot)} : {LocalRoot}");
            Log($"{nameof(KeyPrefix)} : {KeyPrefix}");
            Log($"{nameof(IgnoreKeyPrefix)} : {IgnoreKeyPrefix}");
            Log($"{nameof(ExcludeFiles)} : {ExcludeFiles?.ToJoinedString(",")}");
            Log($"{nameof(ExcludeDirectories)} : {ExcludeDirectories?.ToJoinedString(",")}");
            Log($"{nameof(Silent)} : {Silent}");
            Log($"{nameof(CredentialProfile)} : {CredentialProfile}");

            // Validate Required arguments
            if (string.IsNullOrWhiteSpace(BucketName))
            {
                Error("Please pass arguments. See detail followings.");
                PrintHelp();
                throw new NullReferenceException(nameof(BucketName));
            }

            if (string.IsNullOrWhiteSpace(LocalRoot))
            {
                Error("Please pass arguments. See detail followings.");
                PrintHelp();
                throw new NullReferenceException(nameof(LocalRoot));
            }
        }

        private static string GetEnvValueString(ArgumentType arg, EnvType env)
        {
            var result = Environment.GetEnvironmentVariable(env.ToString());
            if (!string.IsNullOrEmpty(result))
            {
                Warn($"Missing Argument : {arg.ToString()}, overriding with existing Env Value. Env Key : {env}");
            }
            else
            {
                Log($"Missing Argument : {arg.ToString()}, you can override with Env Value. Env Key : {env}");
            }
            return result;
        }

        private static void Error(string text)
        {
            Console.Error.WriteLine(text);
        }

        private static void Warn(string text)
        {
            Log(text, ConsoleColor.DarkYellow);
        }

        private static void PrintHelp()
        {
            Warn($@"-----------------------------------
Arguments
-----------------------------------

Primary use this value. If argument is missing then EnvironmentVariable will override for you.

<Required> {ArgumentType.BucketName}='~'
           (Override envkey : {EnvType.S3Sync_BucketName})
           Synchronize Target S3 Bucket
<Required> {ArgumentType.LocalRoot}='~'
           (Override envkey : {EnvType.S3Sync_LocalRoot})
           Local Synchronization Path

<Optional> {ArgumentType.KeyPrefix}='~'
           (Override envkey : {EnvType.S3Sync_KeyPrefix})
           (default : null)
           Appending S3 Key Prefix on synchronization. Last char '/' will be ignored.
           This prefix will be appended head for each synchronized item so the parent
           or same directory items on S3 will be ignored from synchronization.
<Optional> {ArgumentType.IgnoreKeyPrefix}='~'
           (Override envkey : {EnvType.S3Sync_IgnoreKeyPrefix})
           (default : null)
           Key Prefix for items ignore on S3. Last char '/' will be ignored. S3 path
           for this prefix will be ignore from synchronization.
<Optional> {ArgumentType.ExcludeFiles}=['~','~']
           (Override envkey : {EnvType.S3Sync_ExcludeFiles})
           (default : null)
           Local exclude fileNames. Use , for multiple items.
<Optional> {ArgumentType.ExcludeDirectories}=['~','~']
           (Override envkey : {EnvType.S3Sync_ExcludeDirectories})
           (default : null)
           Local exclude directory Path. Use , for multiple items.
<Optional> {ArgumentType.Silent}=[true|false]
           (Override envkey : {EnvType.S3Sync_Silent})
           (default : false)
           Show upload progress or not.
<Optional> {ArgumentType.CredentialProfile}='~'
           (Override envkey : {EnvType.S3Sync_CredentialProfile})
           (default : null)
           Pass ProfileName. If missing, it expect running with IAM Instance Profile.

-----------------------------------
Examples
-----------------------------------

Example1.
  Synchronize LocalPath 'c:\hoge\moge' with S3Bucket 'MOGEBUCKET'.

  - Full.NET :
    S3Sync.exe {ArgumentType.BucketName}=MOGEBUCKET {ArgumentType.LocalRoot}=c:\hoge\moge

  - .NETCore :
     dotnet S3Sync.dll {ArgumentType.BucketName}=MOGEBUCKET {ArgumentType.LocalRoot}=c:\hoge\moge

Exmaple2.
  Synchronize LocalPath 'c:\hoge\moge' with S3Bucket 'MOGEBUCKET'.
  Ignore local files '.gitignore' and '.gitattributes'.
  Ignore local folder '.git'.

  - Full.NET :
    S3Sync.exe {ArgumentType.BucketName}=MOGEBUCKET {ArgumentType.LocalRoot}=c:\hoge\moge {ArgumentType.ExcludeFiles}=.gitignore,.gitattributes {ArgumentType.ExcludeDirectories}=.git

  - .NETCore :
    dotnet S3Sync.dll {ArgumentType.BucketName}=MOGEBUCKET {ArgumentType.LocalRoot}=c:\hoge\moge {ArgumentType.ExcludeFiles}=.gitignore,.gitattributes {ArgumentType.ExcludeDirectories}=.git

Exmaple3.
  Synchronize LocalPath 'c:\hoge\moge' with S3Bucket 'MOGEBUCKET'.
  Append 'test/fuga/' as KeyPrefix.

  - Full.NET :
    S3Sync.exe {ArgumentType.BucketName}=MOGEBUCKET {ArgumentType.LocalRoot}=c:\hoge\moge {ArgumentType.KeyPrefix}=test/fuga

  - .NETCore :
    dotnet S3Sync.dll {ArgumentType.BucketName}=MOGEBUCKET {ArgumentType.LocalRoot}=c:\hoge\moge {ArgumentType.KeyPrefix}=test/fuga

Exmaple4.
  Synchronize LocalPath 'c:\hoge\moge' with S3Bucket 'MOGEBUCKET'.
  Append 'test/fuga/' as KeyPrefix.
  Ignore existing S3 KeyPrefix 'test/fuga/hoge' items.

  - Full.NET :
    S3Sync.exe {ArgumentType.BucketName}=MOGEBUCKET {ArgumentType.LocalRoot}=c:\hoge\moge {ArgumentType.KeyPrefix}=test/fuga {ArgumentType.IgnoreKeyPrefix}=test/fuga/hoge

  - .NETCore :
    dotnet S3Sync.dll {ArgumentType.BucketName}=MOGEBUCKET {ArgumentType.LocalRoot}=c:\hoge\moge {ArgumentType.KeyPrefix}=test/fuga {ArgumentType.IgnoreKeyPrefix}=test/fuga/hoge

Exmaple5.
  Synchronize LocalPath 'c:\hoge\moge' with S3Bucket 'MOGEBUCKET'.
  Use CredentialProfile name 'hoge'.

  - Full.NET :
    S3Sync.exe {ArgumentType.BucketName}=MOGEBUCKET {ArgumentType.LocalRoot}=c:\hoge\moge CredentialProfile=hoge

  - .NETCore :
    dotnet S3Sync.dll {ArgumentType.BucketName}=MOGEBUCKET {ArgumentType.LocalRoot}=c:\hoge\moge CredentialProfile=hoge
");
        }

        private static void Log(string text)
        {
            Log(text, ConsoleColor.DarkGray);
        }

        private static void Log(string text, TimeSpan elapsed)
        {
            Log($"{text} {elapsed.TotalSeconds.ToRound(2)}sec", ConsoleColor.DarkGray);
        }

        private static void Log(string text, ConsoleColor color)
        {
            lock (typeof(Program))
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

        private static void LogTitle(string text)
        {
            Log($@"
===============================================
{text}
===============================================", ConsoleColor.White);
        }
    }
}
