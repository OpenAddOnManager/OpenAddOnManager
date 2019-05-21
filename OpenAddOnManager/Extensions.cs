using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace OpenAddOnManager
{
    public static class Extensions
    {
        static readonly EnumerationOptions copyContentsEnumerationOptions = new EnumerationOptions { AttributesToSkip = FileAttributes.Hidden | FileAttributes.System };

        public static IReadOnlyList<FileInfo> CopyContentsTo(this DirectoryInfo sourceDirectory, DirectoryInfo targetDirectory, bool overwrite = false)
        {
            var copiedFiles = new List<FileInfo>();
            foreach (var sourceSubDirectory in sourceDirectory.GetDirectories("*.*", copyContentsEnumerationOptions))
            {
                var targetSubDirectory = new DirectoryInfo(Path.Combine(targetDirectory.FullName, sourceSubDirectory.Name));
                if (!targetSubDirectory.Exists)
                    targetSubDirectory.Create();
                copiedFiles.AddRange(CopyContentsTo(sourceSubDirectory, targetSubDirectory, overwrite));
            }
            foreach (var sourceFile in sourceDirectory.GetFiles("*.*", copyContentsEnumerationOptions))
                copiedFiles.Add(sourceFile.CopyTo(Path.Combine(targetDirectory.FullName, sourceFile.Name), overwrite));
            return copiedFiles.ToImmutableArray();
        }
    }
}
