using System;
using System.Collections.Generic;
using HtmlAgilityPack;

namespace AO3EbookDownloader
{
    class AO3LinkHelper
    {
        // A LOT OF THIS IS NOT VERY GOOD PLEASE CLOSE YOUR EYES THANK YOU

        public static Fic GetFicData(string htmlCode)
        {
            // TODO look into more efficient value retrieval and assignment
            var fic = new Fic();
            List<string> formats = new List<string>();
            
            foreach (string fmt in Constants.AllowedExt)
            {
                formats.Add(fmt.ToUpper()); // Not sure if this is necessary, but it worked with uppercase before so nyeh
            }


            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlCode);

            // Single-value attributes
            string title = GetElementValue(htmlDoc, Xpaths.Title).Replace("\n", String.Empty).TrimStart(' ').TrimEnd(' ');
            string lengthString = GetElementValue(htmlDoc, Xpaths.Words);
            int length = int.Parse(lengthString);
            string chaptersString = GetElementValue(htmlDoc, Xpaths.Chapters);
            int chapters;
            try
            {
                chapters = int.Parse(chaptersString.Split('/')[1]);

            }
            catch (Exception)
            {
                chapters = int.Parse(chaptersString.Split('/')[0]);
            }
            string published = GetElementValue(htmlDoc, Xpaths.Published);
            string updated;
            try
            {
                updated = GetElementValue(htmlDoc, Xpaths.Updated);
            }
            catch (Exception)
            {
                updated = published;
            }
            string rating = GetElementValue(htmlDoc, Xpaths.Rating);

            // Multi-value attributes
            var authorElements = htmlDoc.DocumentNode.SelectNodes(Xpaths.Author);
            List<String> authors = new List<String>();
            if (authorElements != null) {
                foreach (HtmlNode authorNode in authorElements)
                {
                    string authorUrl;
                    try
                    {
                        authorUrl = authorNode.Attributes["href"].Value;
                    }
                    catch (Exception)
                    {

                        authorUrl = "";
                    }
                    string authorName;
                    try
                    {
                        authorName = authorNode.InnerText;
                    }
                    catch (Exception)
                    {
                        authorName = "Anonymous";
                    }
                
                    authors.Add(authorName + " : " + authorUrl);
                }
            }
            else
            {
                authors.Add("Anonymous");
            }
            

            SerializableDictionary<string, string> downloadLinks = new SerializableDictionary<string, string>();
            foreach (string format in formats)
            {
                string formatPath = string.Format(Xpaths.Download, format);
                string formatUrl = htmlDoc.DocumentNode.SelectSingleNode(formatPath).Attributes["href"].Value;
                downloadLinks.Add(format, Constants.BaseUrl + formatUrl);
            }

            List<String> categories = new List<String>();
            try
            {
                var categoryElements = htmlDoc.DocumentNode.SelectNodes(Xpaths.Category);
                foreach (HtmlNode catNode in categoryElements)
                {
                    string catName = catNode.InnerText;
                    categories.Add(catName);
                }
            }
            catch (Exception)
            {
            }

            List<String> fandoms = new List<String>();
            try
            {
                var fandomElements = htmlDoc.DocumentNode.SelectNodes(Xpaths.Fandom);
                foreach (HtmlNode fanNode in fandomElements)
                {
                    string fandomName = fanNode.InnerText;
                    fandoms.Add(fandomName);
                }
            }
            catch (Exception)
            {
            }

            List<String> relationships = new List<String>();
            try
            {
                var relElements = htmlDoc.DocumentNode.SelectNodes(Xpaths.Relationship);
                foreach (HtmlNode relNode in relElements)
                {
                    string relName = relNode.InnerText;
                    relationships.Add(relName);
                }
            }
            catch (Exception)
            {
            }

            List<String> characters = new List<String>();
            try
            {
                var charElements = htmlDoc.DocumentNode.SelectNodes(Xpaths.Characters);
                foreach (HtmlNode charNode in charElements)
                {
                    string charName = charNode.InnerText;
                    characters.Add(charName);
                }
            }
            catch (Exception)
            {
            }

            List<String> tags = new List<String>();
            try
            {
                var tagElements = htmlDoc.DocumentNode.SelectNodes(Xpaths.Tags);
                foreach (HtmlNode tagNode in tagElements)
                {
                    string tagName = tagNode.InnerText;
                    tags.Add(tagName);
                }
            }
            catch (Exception)
            {
            }

            List<string> summary = new List<string>();
            try
            {
                var parElements = htmlDoc.DocumentNode.SelectNodes(Xpaths.Summary);
                foreach (HtmlNode parNode in parElements)
                {
                    string line = parNode.InnerText;
                    summary.Add(line);
                }
            }
            catch (Exception)
            {
            }


            // Property assignment
            fic.Title = title;
            fic.Words = length;
            fic.Chapters = chapters;
            fic.Downloads = downloadLinks;
            fic.Published = published;
            fic.Updated = updated;
            fic.Rating = rating;
            fic.Author = authors.ToArray();
            fic.Category = categories.ToArray();
            fic.Fandom = fandoms.ToArray();
            fic.Relationship = relationships.ToArray();
            fic.Characters = characters.ToArray();
            fic.AdditionalTags = tags.ToArray();

            fic.Summary = String.Join(Environment.NewLine, summary);


            return fic;
        }

        private static String GetElementValue(HtmlDocument hDoc, string xpath)
        {
            // Hide ugly function here because  a e s t h e t i c
            return hDoc.DocumentNode.SelectSingleNode(xpath).InnerText;
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
