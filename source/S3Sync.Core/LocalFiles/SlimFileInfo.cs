using System.IO;
using System.Linq;

namespace S3Sync.Core.LocalFiles
{
    public struct SlimFileInfo
    {
        public string FullPath { get; private set; }
        public string DirectoryName { get; private set; }
        public string FileName { get; private set; }
        public string RelativePath { get; private set; }
        public string MultiplatformRelativePath { get; private set; }
        public string RelativeDirectory { get; private set; }
        public string MultiplatformRelativeDirectory { get; private set; }

        public SlimFileInfo(string fullPath, string basePath)
        {
            FullPath = fullPath;
            DirectoryName = Path.GetDirectoryName(fullPath);
            FileName = Path.GetFileName(fullPath);

            var tempRelativePath = fullPath.Replace(basePath, "");
            RelativePath = tempRelativePath.First() != '\\' ? tempRelativePath : tempRelativePath.Substring(1, tempRelativePath.Length - 1);
            MultiplatformRelativePath = RelativePath.Replace(@"\", "/");

            var tempRelativeDirectory = RelativePath.Replace(FileName, "");
            if (string.IsNullOrEmpty(tempRelativeDirectory))
            {
                RelativeDirectory = string.Empty;
                MultiplatformRelativeDirectory = string.Empty;
            }
            else
            {
                RelativeDirectory = tempRelativeDirectory.Last() != '\\' ? tempRelativeDirectory : tempRelativeDirectory.Substring(0, tempRelativeDirectory.Length - 1);
                MultiplatformRelativeDirectory = RelativeDirectory.Replace(@"\", "/");
            }
        }
    }
}
