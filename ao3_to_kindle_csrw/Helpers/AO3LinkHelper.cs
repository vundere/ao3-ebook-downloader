using System;
using System.Collections.Generic;
using HtmlAgilityPack;

namespace AO3EbookDownloader
{
    class AO3LinkHelper
    {

        private static string xpathTemplateTitle = "//h2[@class=\"title heading\"]"; // //text() can be appended to select text directly
        private static string xpathTemplateWords = "//dd[@class=\"words\"]"; // //text() can be appended to select text directly
        private static string xpathTemplateDownload = "//a[text() = \"{0}\"]";

        public static Fic GetFicData(string htmlCode, List<String> formats)
        {
            var fic = new Fic();

            HtmlDocument htmlDoc = new HtmlDocument();
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

        public static String ProceedUrl(string htmlCode)
        {
            string newUrl = "";

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlCode);

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
