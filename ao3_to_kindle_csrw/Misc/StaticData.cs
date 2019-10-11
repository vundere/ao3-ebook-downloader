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

        public static readonly String GitHubUrl = "https://github.com/vundere/ao3-ebook-downloader";

        public static readonly String KindleList = Directory.GetParent(Assembly.GetExecutingAssembly().Location) + @"\XML Files\KindleList.xml";

        public static readonly String[] AllowedExt = new String[] {"mobi", "epub", "azw3", "pdf"};  // ".html" removed from list because it catches a bunch of system files

        public static readonly String LibHashList = Directory.GetParent(Assembly.GetExecutingAssembly().Location) + @"\XML Files\LibHash.xml";

        public static readonly String WorksRef = Directory.GetParent(Assembly.GetExecutingAssembly().Location) + @"\XML Files\Works.xml";

        public static readonly String LibRef = Directory.GetParent(Assembly.GetExecutingAssembly().Location) + @"\XML Files\LibraryFolders.xml";

        public static readonly String LiteDbPath = Directory.GetParent(Assembly.GetExecutingAssembly().Location) + @"\application_data.db";

        public static readonly String GreenClr = "#7DCEA0";

        public static readonly String RedClr = "#D98880";
    }
}
