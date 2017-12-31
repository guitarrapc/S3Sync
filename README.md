# S3Sync

Amazon S3 Content Synchronization with .NET.

S3Sync synchronize a directory to a S3 Bucket. It meakes bucket identical to the `LocalRoot` (source).

Note: Remote files that are not in the `LocalRoot` are removed.

## How to use.

You can download latest version from [Release](https://github.com/guitarrapc/S3Sync/releases) Page.

Action | Full.NET  | .NETCore 2.0 | Docker
---- | ---- | ---- | ----
Requirement | .NETFreamework 4.7 or higher | [.NETCore 2.0 or higher](https://www.microsoft.com/net/download/windows) | Docker
Download | `s3sync_netfull.zip` | `s3sync_netcore.tar.gz` | [guitarrapc/s3sync](https://hub.docker.com/r/guitarrapc/s3sync/)
Run | Extract zip and run `S3Sync.exe` | Extract zip and run `dotnet S3Sync.dll` | `docker run guitarrapc/s3sync`

## Configuration

You can pass parameter to S3Sync with `Arguments` or `Environment Variable`.

Arguments | Environment Variable | Required?<br/>Optional? | Description
---- | ---- | ---- | ----
BucketName=`"string"` | S3Sync_BucketName | Required | Specify S3 BucketName to sync.
LocalRoot=`"string"` | S3Sync_LocalRoot | Required | Specify Local File Path to Sync.
KeyPrefix=`"string"` | S3Sync_KeyPrefix | Optional | Specify KeyPrefix to add to localfile when Sync.
IgnoreKeyPrefix=`"string"` | S3Sync_IgnoreKeyPrefix | Optional | Specify KeyPrefix to ignore on S3.
ExcludeFiles=`"string","string"` | S3Sync_ExcludeFiles | Optional | Specify local file names you want to exclude. (use `,` for multiple.)
ExcludeDirectories=`"string","string"` | S3Sync_ExcludeDirectories | Optional | Specify local directory names you want to exclude. (use `,` for multiple.)
CredentialProfile=`"string"` | S3Sync_CredentialProfile | Optional | Specify Credential Profile name.
Silent=`bool` | S3Sync_Silent | Optional | Set `true` when you want to supress upload progress. (Default : `false`)

## Sample

You can use `dotnet` to run as .NETCore.

```bash
$ dotnet S3Sync.dll BucketName=your-awesome-bucket LocalRoot=/Home/User/HogeMoge ExcludeFiles=.gitignore,.gitattributes ExcludeDirectories=.git,test
```

No .NETCore? You can use Full.NET as a ConsoleApp.

```cmd
S3Sync.exe BucketName=your-fantastic-bucket KeyPrefix=hoge LocalRoot=C:/Users/User/HomeMoge
```

## Docker Support

You can run with docker.

Run with IAM Role is recommended.

```bash
docker run --rm -v <YOUR_SYNC_DIR>:/app/sync/ -e S3Sync_BucketName=<YOUR_BUCKET_NAME> s3sync
```

Local run without IAM Role, use AWS Credentials.

```bash
$ docker run --rm -v <YOUR_SYNC_DIR>:/app/sync/ -e S3Sync_BucketName=<YOUR_BUCKET_NAME> -e AWS_ACCESS_KEY_ID=<YOUR_ACCESS_KEY> -e AWS_SECRET_ACCESS_KEY=<YOUR_SECRET> s3sync
```

### Build s3sync within docker

Build S3Sync with docker-compose. This enable you not to think about .NETCore2.0 sdk installed on your host.

```bash
docker-compose -f docker-compose.ci.build.yml up
```

Build artifacts will be generated in following path.

```bash
S3Sync\source\S3Sync\obj\Docker\publish
```

Clean up build docker container resource with down.

```bash
docker-compose -f docker-compose.ci.build.yml down
```

### Docker Image Build

Create docker image with docker-compose.

```bash
docker-compose -f docker-compose.yml build
```

## Credential handling

Synchronization operation requires read, write and delete objects permission.

It is recommended that you use `IAM Policy` and `Profile` to handle appropriate access right.

### Configure IAM Policy

Here's some sample IAM Policy.

```json
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Sid": "Stmt1446117060000",
            "Effect": "Allow",
            "Action": [
                "s3:GetObject",
                "s3:ListAllMyBuckets",
                "s3:ListBucket",
                "s3:PutObject"
            ],
            "Resource": [
                "arn:aws:s3:::*"
            ]
        }
    ]
}
```

If you want to restrict access to certain Bucket, then replace `*` with desired bucketName.

```json
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Sid": "Stmt1446117060000",
            "Effect": "Allow",
            "Action": [
                "s3:GetObject",
                "s3:ListAllMyBuckets",
                "s3:ListBucket",
                "s3:PutObject"
            ],
            "Resource": [
                "arn:aws:s3:::PutYourBucketName"
            ]
        }
    ]
}
```

### Configure Profile

There are several way to set profile.

If you run S3Sync on AWS Resource, you should AWS managed profile like IAM Instance Profile.
If you run S3Sync on Local Environment, you can configure your machine with "aws cli" or otheres.

#### aws cli onfigure sample

You can create AWS Credential Profile with AWS CLI.

```bash
aws configure --profile sample
```

#### Other options?

You can create Profile with other tools.

> - [AWS Toolkit for Visual Studio](https://aws.amazon.com/visualstudio/?nc1=f_ls) .
> - [AWS Tools for Windows PowerShell](https://aws.amazon.com/powershell/?nc1=f_ls)

Or you can use following method.

```csharp
public static void RegisterProfile(string profileName, string accessKey, string accessSecret)
{
    var option = new CredentialProfileOptions
    {
        AccessKey = accessKey,
        SecretKey = accessSecret
    };
    new NetSDKCredentialsFile().RegisterProfile(new CredentialProfile(profileName, option));
}
```

## License

The MIT License (MIT)