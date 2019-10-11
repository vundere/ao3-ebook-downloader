using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;

namespace AO3EbookDownloader
{
    //[XmlRoot("Work")]
    public class Fic
    {
        public string Title { get; set; }
        public int Words { get; set; }
        public int Chapters { get; set; }
        public string[] Author { get; set; }
        public string Published { get; set; }
        public string Updated { get; set; }
        public string Rating { get; set; }
        public string[] Category { get; set; }
        public string[] Fandom { get; set; }
        public string[] Relationship { get; set; }
        public string[] Characters { get; set; }
        public string[] AdditionalTags { get; set; }
        public string Series { get; set; }
        public string Summary { get; set; }

        public SerializableDictionary<string, string> Downloads { get; set; }

        public SerializableDictionary<string, string> LocalFiles = new SerializableDictionary<string, string>();


        // Only these two because those are the formats a Kindle will read
        public bool MobiOnKindle { get; set; }

        public bool Azw3OnKindle { get; set; }


        private string _Url;
        public string Url
        {
            get { return _Url; }
            set
            {
                _Url = value;
                _ID = GetID(value);
            }
        }

        private string _ID;  // TODO set this when setting Url instead of having its own setter.
        public string ID
        {
            get { return _ID; }
            set
            {
                if (!String.IsNullOrEmpty(_Url))
                {
                    _ID = GetID(_Url);
                }
                else
                {
                    _ID = null;
                }

            } // Deprecated...
        }

        private string GetID(string value)
        {
            if (value.StartsWith("http"))
            {
                Uri workUri = new Uri(value);
                return workUri.Segments.Last();
            }
            else
            {
                return "";
            }
        }

        public static Fic Merge(Fic one, Fic two)
        {
            // This might need an update later
            DateTime dateOne = DateTime.Parse(one.Updated);
            DateTime dateTwo = DateTime.Parse(two.Updated);


            if (dateOne < dateTwo)
            {
                foreach (KeyValuePair<string, string> pair in one.LocalFiles)
                {
                    if (!two.LocalFiles.ContainsKey(pair.Key))
                    {
                        two.LocalFiles[pair.Key] = pair.Value;
                    }
                }
            }

            return two;
        }
    }
}
