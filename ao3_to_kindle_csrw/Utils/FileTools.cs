using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AO3EbookDownloader
{
    internal static class FileTools
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
            fileName = fileName.Replace("%20", " ");
            

            return fileName;
        }
        public static string[] Walker(List<string> folders)
        {
            Stack<string> dirs = new Stack<string>(folders);
            List<string> allFiles = new List<string>();



            while (dirs.Count > 0)
            {
                string folder = dirs.Pop();
                string[] subDirs;
                try
                {
                    subDirs = Directory.GetDirectories(folder);
                    //MetadataHandler.AddLib(folder);
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }
                catch (DirectoryNotFoundException)
                {
                    continue;
                }
                string[] files;
                try
                {
                    files = Directory.GetFiles(folder);
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }
                catch (DirectoryNotFoundException)
                {
                    continue;
                }

                foreach (string file in files)
                    allFiles.Add(file);

                foreach (string subDir in subDirs)
                    dirs.Push(subDir);

            }

            return allFiles.ToArray();
        }

        public static List<string> MatchHashes(Dictionary<string, string> hashDict, Dictionary<string, Fic> objDict)
        {
            /*
             * hashDict is in the format 'MD5 hash sum': 'file path'
             * objDict is in the format 'work ID': 'Fic object'
             */
            List<string> lF = new List<string>();
            Dictionary<string, string> lookup = new Dictionary<string, string>();

            foreach (KeyValuePair<string, Fic> pair in objDict)
            {
                foreach (KeyValuePair<string, string> lfPair in pair.Value.LocalFiles)
                {

                    string hash = WorkIdent.Hash(lfPair.Value);
                    lookup[hash] = pair.Key;
                }
            }

            var matches = lookup.Keys.Intersect(hashDict.Keys);

            foreach (var m in matches)
            {
                lF.Add(lookup[m]);
            }

            return lF;
        }

    }
}
