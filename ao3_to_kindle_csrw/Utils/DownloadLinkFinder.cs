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

            url = url + "?view_adult=true";
            HtmlWeb web = new HtmlWeb();
            var htmlDoc = web.Load(url);

            try
            {
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
            }
            catch
            {
                fic = GetFic(ProceedUrl(url), formats);
            }
            return fic;
        }

        private static String ProceedUrl (String url)
        {
            string newUrl = url;
            HtmlWeb web = new HtmlWeb();
            var htmlDoc = web.Load(url);

            string xpathToProceedButton = "//a[text() = \"Proceed\"]";

            try
            {
                string redirLink = htmlDoc.DocumentNode.SelectSingleNode(xpathToProceedButton).Attributes["href"].Value;
                if (redirLink != "")
                {
                    newUrl = Constants.BaseUrl + redirLink;
                }
            }
            catch
            {
                // this just means that we're already where we want to be, and no further navigation is needed.
            }

            return newUrl;
        }
    }
}
