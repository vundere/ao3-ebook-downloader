using System;
using System.Xml;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Xml.XPath;
using System.Threading.Tasks;

namespace AO3EbookDownloader
{
    class XmlOperator
    {
        // Just here to save typing 'Constants.' every time...
        static readonly String worksRef = Constants.WorksRef;  

        static readonly String libRef = Constants.LibRef;


        internal static SerializableDictionary<string, Fic> LoadFics()
        {
            SerializableDictionary<string, Fic> res = new SerializableDictionary<string, Fic>();
            try
            {
                res = DeserializeFile<SerializableDictionary<string, Fic>>(worksRef);
            }
            catch (Exception)
            {

            }
            
            return res;
        }

        public static void StoreWorks(SerializableDictionary<string, Fic> works)
        {
            Serialize(works, Constants.WorksRef);
        }

        public static void AddWork(Fic work)
        {
            ;
        }

        public static T DeserializeFile<T>(string filepath)
        {
            try
            {
                using (var reader = new StreamReader(filepath))
                {
                    XmlSerializer deserializer = new XmlSerializer(typeof(T), new XmlRootAttribute("root"));
                    T result = (T)deserializer.Deserialize(reader);
                    return result;
                }
            }
            catch (Exception)
            {
                throw;
                return default;
            }
            
        }

        public static void Serialize<T>(T obj, string filename)
        {
            using (FileStream writer = new FileStream(filename, FileMode.Create))
            {
                XmlSerializer ser = new XmlSerializer(typeof(T), new XmlRootAttribute("root"));
                ser.Serialize(writer, obj);
            }
        }

        public static void AddLib(string fpath)
        {
            List<string> existingLib;
            try
            {
                existingLib = DeserializeFile<List<string>>(libRef);
                if (existingLib == null)
                {
                    existingLib = new List<string>();
                }
            }
            catch (InvalidOperationException)
            {
                existingLib = new List<string>();
            }
            if (!existingLib.Contains(fpath))
            {
                existingLib.Add(fpath);
                Serialize(existingLib, libRef);
            }
        }

        public static List<string> GetFolders()
        {
            List<string> folders;
            try
            {
                folders = DeserializeFile<List<string>>(libRef);
                if (folders == null)
                {
                    folders = new List<string>();
                }
            }
            catch (Exception)
            {
                folders = new List<string>();
            }

            return folders;
        }

        #region Deprecated
        // This is a collection of deprecated methods kept around until next git push just to have a reference in case I suddenly change my mind and need them again...

        public static void AddWorkOld(Fic work)
        {
            // Deprecated, use Serialize instead.
            if (!File.Exists(worksRef))
            {
                GenerateMetadataStore();
            }

            XmlDocument doc = new XmlDocument();
            doc.Load(worksRef);

            XmlElement newData = doc.CreateElement("Work"); // Not sure if this is the best title for the individual work node, but it'll do for now
            newData.SetAttribute("id", work.ID);  // Setting the ID as an attribute as well, for easier searching (probably?)

            /* 
             * Iterates over the Fic object's properties and writes them to XML
             * Yes, LINQ does this prettier, but that'd require hardcoding all the properties.
             * Switching this out for a LINQ to XML statement once things start working properly
             * might be a good idea, though.
             */
            PropertyInfo[] properties = typeof(Fic).GetProperties();
            foreach (PropertyInfo property in properties)
            {
                XmlElement innerData = doc.CreateElement(nameof(property));
                innerData.InnerText = property.GetValue(work).ToString();
                newData.AppendChild(innerData);
            }

            doc.Save(worksRef);
        }
        public static T DeserializeXdoc<T>(XmlDocument doc)
        {
            // Deprecated... Remains here for reference, to be removed later.
            using (XmlReader reader = new XmlNodeReader(doc.GetElementsByTagName("Works")[0].FirstChild))
            {
                var ser = new XmlSerializer(typeof(T));
                T result = (T)ser.Deserialize(reader);
                return result;
            }
        }
        public static void OldBulkSerialize(List<Fic> fics)
        {
            XmlDocument doc = new XmlDocument();

            if (!File.Exists(worksRef))
            {
                // Declaration
                XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
                XmlElement root = doc.DocumentElement;
                doc.InsertBefore(xmlDeclaration, root);
                XmlElement worksEle = doc.CreateElement(string.Empty, "Works", string.Empty);
                doc.AppendChild(worksEle);
            }
            else
            {
                doc.Load(worksRef);
            }

            XPathNavigator nav;
            try
            {
                var rootNode = doc.GetElementsByTagName("Works")[0];
                nav = rootNode.CreateNavigator();
            }
            catch (Exception)
            {
                nav = doc.CreateNavigator();
            }

            if (nav != null)
            {
                XmlWriterSettings settings = new XmlWriterSettings
                {
                    OmitXmlDeclaration = true,
                    ConformanceLevel = ConformanceLevel.Fragment,
                    Indent = true,
                    NamespaceHandling = NamespaceHandling.OmitDuplicates
                };
                using (XmlWriter writer = XmlWriter.Create(nav.AppendChild(), settings))
                {
                    writer.WriteWhitespace("");
                    foreach (Fic fic in fics)
                    {
                        var ser = new XmlSerializer(fic.GetType());
                        XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                        ns.Add("", "");
                        ser.Serialize(writer, fic, ns);
                    }
                }
            }
            doc.Save(worksRef);
        }
        public static void OldSerialize(Fic fic)
        {
            // BulkSerialize is way faster, use that instead.
            XmlDocument doc = new XmlDocument();

            if (!File.Exists(worksRef))
            {
                GenerateMetadataStore();
            }
            else
            {
                doc.Load(worksRef);
            }

            XPathNavigator nav;
            try
            {
                var rootNode = doc.GetElementsByTagName("Works")[0];
                nav = rootNode.CreateNavigator();
            }
            catch (Exception)
            {
                nav = doc.CreateNavigator();
            }

            if (nav != null)
            {
                XmlWriterSettings settings = new XmlWriterSettings
                {
                    OmitXmlDeclaration = true,
                    ConformanceLevel = ConformanceLevel.Fragment,
                    Indent = true,
                    NamespaceHandling = NamespaceHandling.OmitDuplicates
                };
                using (XmlWriter writer = XmlWriter.Create(nav.AppendChild(), settings))
                {
                    writer.WriteWhitespace("");
                    var ser = new XmlSerializer(fic.GetType());
                    XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                    ns.Add("", "");
                    ser.Serialize(writer, fic, ns);
                }
            }
            doc.Save(worksRef);

        }

        static void GenerateMetadataStore()
        {
            XmlDocument doc = new XmlDocument();
            // Declaration
            XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            XmlElement root = doc.DocumentElement;
            doc.InsertBefore(xmlDeclaration, root);

            XmlElement rootEle = doc.CreateElement(string.Empty, "root", string.Empty);
            doc.AppendChild(rootEle);

            XmlElement worksEle = doc.CreateElement(string.Empty, "Works", string.Empty);
            doc.DocumentElement.AppendChild(worksEle);

            XmlElement libEle = doc.CreateElement(string.Empty, "Library", string.Empty);
            doc.DocumentElement.AppendChild(libEle);

            doc.Save(worksRef);
        }
        #endregion Deprecated

    }
}
