using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AO3EbookDownloader
{
    class HRT
    {
        /*
         * Class to contain methods for transitioning between data-store solutions etc.
         */


        public static void DbCheck()
        {
            if (!File.Exists(Constants.LiteDbPath))
            {
                string[] xmlFiles = { Constants.LibRef, Constants.LibHashList, Constants.KindleList, Constants.WorksRef };

                bool noFiles = true;

                foreach (string fpath in xmlFiles)
                {
                    if (File.Exists(fpath))
                    {
                        noFiles = false;
                    }
                }

                if (!noFiles)
                {
                    XmlToLdb();
                }
            }
        }

        public static void XmlToLdb()
        {
            SerializableDictionary<string, Fic> fics;
            List<string> lib = new List<string>();
            SerializableDictionary<string, string> libHash;
            SerializableDictionary<string, string> kindleFiles;

            Dictionary<string, Fic> nDictFics = new Dictionary<string, Fic>();
            Dictionary<string, string> nDictLibHash = new Dictionary<string, string>();
            Dictionary<string, string> nDictKindle = new Dictionary<string, string>();

            bool success = false;

            try
            {
                fics = XmlOperator.LoadFics();
                nDictFics = new Dictionary<string, Fic>(fics);
                success = true;
            }
            catch (Exception)
            {
            }

            try
            {
                lib = XmlOperator.GetFolders();
            }
            catch (Exception)
            {
            }

            try
            {
                libHash = XmlOperator.DeserializeFile<SerializableDictionary<string, string>>(Constants.LibHashList);
                nDictLibHash = new Dictionary<string, string>(libHash);
                success = true;
            }
            catch (Exception)
            {
            }

            try
            {
                kindleFiles = XmlOperator.DeserializeFile<SerializableDictionary<string, string>>(Constants.KindleList);
                nDictKindle = new Dictionary<string, string>(kindleFiles);
                success = true;
            }
            catch (Exception)
            {
            }

            if (success)
            {
                try
                {
                    Liter.StoreFics(nDictFics);
                }
                catch (Exception)
                {
                }

                try
                {
                    foreach (string fldrpath in lib)
                        Liter.AddLib(fldrpath);
                }
                catch (Exception)
                {
                    throw;
                }

                try
                {
                    // LibHash
                    // Not implemented
                }
                catch (Exception)
                {
                }

                try
                {
                    //Kindle
                    Liter.StoreKindleHashes(nDictKindle);
                }
                catch (Exception)
                {
                    throw;
                }
            }


        }
    }
}
