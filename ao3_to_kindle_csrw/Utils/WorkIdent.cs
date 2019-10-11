using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AO3EbookDownloader
{
    class WorkIdent
    {
        // This probably doesn't need to be its own class, but for now it is just to make it easier to re-use and swap out if needed
        public static string libIdent = Constants.LibHashList;  // Just for prettier code later


        public static void GenerateLibIdent()
        {
            XmlDocument doc = new XmlDocument();
            // Declaration
            XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            XmlElement root = doc.DocumentElement;
            doc.InsertBefore(xmlDeclaration, root);

            XmlElement rootEle = doc.CreateElement(string.Empty, "root", string.Empty);
            doc.AppendChild(rootEle);

            doc.Save(libIdent);
        }

        public static string Hash(string filename)
            // Shamelessly copied off StackExchange
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        public static void Store(string workId, string hash)
        {
            if (!File.Exists(libIdent))
            {
                GenerateLibIdent();
            }

            XmlDocument doc = new XmlDocument();
            doc.Load(libIdent);

            
            var newData = doc.CreateElement("IdHashPair");
            newData.SetAttribute("id", workId);
            newData.SetAttribute("hash", hash);
            newData.SetAttribute("date", DateTime.Today.ToShortDateString());

            var rootNode = doc.SelectSingleNode("root");
            rootNode.InsertAfter(newData, rootNode.LastChild);
        }

    }
}
