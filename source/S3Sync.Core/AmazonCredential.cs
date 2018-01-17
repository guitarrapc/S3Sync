using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.Util;
using System;

namespace S3Sync.Core
{
    /// <summary>
    /// Best practice. Run with IAM Role. like IAM Instance Profile.
    /// ----------------------------------
    /// var s3 = new S3Client();
    ///
    /// Local run, using AccessKey, AccessSecret in Environment Variable.
    /// ----------------------------------
    /// AWS_ACCESS_KEY_ID="YOUR_ACCES_KEY"
    /// AWS_SECRET_ACCESS_KEY="YOUR_ACCESS_SECRET"
    /// var s3 = new S3Client();
    ///
    /// Local run, using AWS CredentialProfile.
    /// ----------------------------------
    /// var s3 = new S3Client(AmazonCredential.GetCredential("Credential_Profile_Name"));
    ///
    /// Not recommend. Use AccessKey.
    /// ----------------------------------
    /// AmazonCredential.RegisterProfile("Credential_Profile_Name", "accessKey", "accessSecret");
    /// var s3 = new S3Client(AmazonCredential.GetCredential("Credential_Profile_Name"));
    /// </summary>
    public static class AmazonCredential
    {
        public static AWSCredentials GetCredential(string profileName)
        {
            var chain = new CredentialProfileStoreChain();
            if (chain.TryGetProfile(profileName, out var profile) && chain.TryGetAWSCredentials(profileName, out var credentials))
            {
                return credentials;
            }
            throw new NullReferenceException($"{nameof(profileName)} not found from exsiting profile list. Make sure you have set Profile");
        }

        public static void RegisterProfile(string profileName, string accessKey, string accessSecret)
        {
            var option = new CredentialProfileOptions
            {
                AccessKey = accessKey,
                SecretKey = accessSecret
            };
            new NetSDKCredentialsFile().RegisterProfile(new CredentialProfile(profileName, option));
        }
    }
}
