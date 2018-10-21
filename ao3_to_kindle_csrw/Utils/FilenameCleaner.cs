using System;
using System.Text.RegularExpressions;

namespace AO3EbookDownloader
{
    internal static class FilenameCleaner
    {
        public static String ToAllowedFileName(this String fileName)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            fileName = fileName.Replace("\\", "_");
            fileName = fileName.Replace("/", "_");
            fileName = fileName.Replace(":", "_");
            fileName = fileName.Replace("*", "_");
            fileName = fileName.Replace("?", "_");
            fileName = fileName.Replace("\"", "_");
            fileName = fileName.Replace("<", "_");
            fileName = fileName.Replace(">", "_");
            fileName = fileName.Replace("|", "_");
            fileName = fileName.Replace(Environment.NewLine, "_");
            fileName = Regex.Replace(fileName, @"\s+", " ");

            return fileName;
        }
    }
}
