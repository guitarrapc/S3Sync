using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.Util;
using System;

namespace S3Sync.Core.Diagnostics
{
    public class ExponentialBackoff
    {
        readonly Random random;
        readonly double minBackoffMilliseconds;
        readonly double maxBackoffMilliseconds;
        readonly double deltaBackoffMilliseconds;

        int currentPower;

        public ExponentialBackoff(TimeSpan minBackoff, TimeSpan maxBackoff, TimeSpan deltaBackoff)
        {
            random = new Random();
            minBackoffMilliseconds = minBackoff.TotalMilliseconds;
            maxBackoffMilliseconds = maxBackoff.TotalMilliseconds;
            deltaBackoffMilliseconds = deltaBackoff.TotalMilliseconds;
        }

        public TimeSpan GetNextDelay()
        {
            int delta = (int)((System.Math.Pow(2.0, currentPower) - 1.0) * random.Next((int)(deltaBackoffMilliseconds * 0.8), (int)(deltaBackoffMilliseconds * 1.2)));
            int interval = (int)System.Math.Min(checked(minBackoffMilliseconds + delta), maxBackoffMilliseconds);

            if (interval < maxBackoffMilliseconds)
            {
                currentPower++;
            }

            return TimeSpan.FromMilliseconds(interval);
        }

        public static class Preset
        {
            /// <summary>
            /// Preset for AWS Retry : 00:00:01, 00:00:03, 00:00:07, 00:00:15, 00:00:30...
            /// </summary>
            /// <returns></returns>
            public static ExponentialBackoff AwsOperation()
            {
                return new ExponentialBackoff(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2));
            }
        }
    }
}
