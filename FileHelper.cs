using System;
using System.IO;
using System.Runtime.InteropServices;

namespace MigrateDocuments
{
    public static class FileHelper
    {
        public static string RemoveInvalidFileNameChars(string filename)
        {
            return string.Concat(filename.Split(Path.GetInvalidFileNameChars()));
        }

        public static string TrimPathLength(string path, int length)
        {
            length -= 1; // Make space for terminating NUL character

            if (path.Length <= length)
                return path;

            string extension = "";

            if (Path.HasExtension(path))
            {
                extension = Path.GetExtension(path);
            }

            path = path.Substring(0, length - 3 - extension.Length);

            // File name cannot end in a dot on Windows
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && string.IsNullOrEmpty(extension))
            {
                return path;
            }

            return $"{path}...{extension}";
        }
    }
}
