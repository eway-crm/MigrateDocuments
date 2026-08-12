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
            const string Ellipsis = "...";

            if (path.Length <= length)
                return path;

            string extension = Path.HasExtension(path) ? Path.GetExtension(path) : "";

            // Guard: a "FileAs"/subject may contain a dot with a long tail, so
            // Path.GetExtension can return more than fits. Fall back to a plain trim.
            if (length - Ellipsis.Length - extension.Length < 0)
            {
                extension = "";
            }

            path = path.Substring(0, length - Ellipsis.Length - extension.Length);

            // File name cannot end in a dot on Windows
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && string.IsNullOrEmpty(extension))
            {
                return path;
            }

            return $"{path}...{extension}";
        }
    }
}
