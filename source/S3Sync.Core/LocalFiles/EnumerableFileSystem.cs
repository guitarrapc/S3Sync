using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace S3Sync.Core.LocalFiles
{
    public class EnumerableFileSystem
    {
        public IEnumerable<SlimFileInfo> Files { get; private set; }
        public string[] ExcludeFiles { get; private set; }
        public string[] ExcludeDirectories { get; private set; }

        public EnumerableFileSystem()
        {
        }

        public EnumerableFileSystem(string[] excludeDirectories = null, string[] excludeFiles = null)
        {
            ExcludeDirectories = excludeDirectories;
            ExcludeFiles = excludeFiles;
        }

        public IEnumerable<SlimFileInfo> EnumerateFiles(string filePath, string pattern = "*", SearchOption option = SearchOption.AllDirectories)
        {
            Files = Directory.EnumerateFiles(filePath, pattern, option)
                .Select(x => new SlimFileInfo(x, filePath));

            // Exclude files under specified directories (respect directory structure)
            if (ExcludeDirectories != null && ExcludeDirectories.Any())
            {
                Files = Files.Where(x => !ExcludeDirectories.Any(y => x.MultiplatformRelativeDirectory.StartsWith(y)));
            }

            // Exclude specified name files (Just matching filename, ignore directory structure.)
            if (ExcludeFiles != null && ExcludeFiles.Any())
            {
                Files = Files.Where(x => !ExcludeFiles.Contains(x.FileName));
            }
            return Files;
        }

        public void Save()
        {
            // TODO : Save enumerate result cache to file
            throw new NotImplementedException();
        }

        public void Load()
        {
            // TODO : read enumerate result cache from file
            throw new NotImplementedException();
        }
    }
}
