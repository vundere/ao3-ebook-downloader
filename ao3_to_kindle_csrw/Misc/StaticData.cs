using System;
using System.IO;
using System.Reflection;

namespace AO3EbookDownloader
{
    internal static class Constants
    {
        public static readonly String PasteBoxDefText = $"Paste links here, separated by line breaks.{Environment.NewLine}Links to individual works only, links to series, bookmarks etc. will not work.";

        public static readonly String SettingsPath = Directory.GetParent(Assembly.GetExecutingAssembly().Location) + @"\Settings.ini";

        public static readonly String BaseUrl = "https://archiveofourown.org";
    }
}
