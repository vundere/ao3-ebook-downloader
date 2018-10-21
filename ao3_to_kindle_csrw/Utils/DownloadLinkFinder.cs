using System;
using System.Collections.Generic;
using System.Net;
using HtmlAgilityPack;

namespace AO3EbookDownloader
{
    internal static class DownloadLinkFinder
    {
        public static Fic GetFic(String url, List<String> formats)
        {
            Fic fic = new Fic();
            string xpathTemplateTitle = "//h2[@class=\"title heading\"]"; // //text() can be appended to select text directly
            string xpathTemplateWords = "//dd[@class=\"words\"]"; // //text() can be appended to select text directly
            string xpathTemplateDownload = "//a[text() = \"{0}\"]";

            string htmlCode = GetHtmlString(url);
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlCode);
            
            string title = htmlDoc.DocumentNode.SelectSingleNode(xpathTemplateTitle).InnerText;
            string lengthString = htmlDoc.DocumentNode.SelectSingleNode(xpathTemplateWords).InnerText;
            int length = int.Parse(lengthString);

            fic.Title = title;
            fic.Length = length;

            Dictionary<string, string> downloadLinks = new Dictionary<string, string>();
            foreach (string format in formats)
            {
                string formatPath = string.Format(xpathTemplateDownload, format);
                string formatUrl = htmlDoc.DocumentNode.SelectSingleNode(formatPath).Attributes["href"].Value;
                downloadLinks.Add(format, Constants.BaseUrl + formatUrl);
            }
            fic.Downloads = downloadLinks;
            return fic;
        }

        private static String GetHtmlString(String url)
        {
            using (WebClient client = new WebClient())
            {
                string htmlCode = client.DownloadString(url+"?view_adult=True");
                return htmlCode;
            }
        }
    }
}
