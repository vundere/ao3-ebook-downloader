using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace AO3EbookDownloader
{
    public class MiddleDude
    {
        public static Dictionary<string, Fic> GetFics()
        {
            var res = Liter.GetFics();

            return res;
            // Leaving this code below temporarily

            var sd = XmlOperator.LoadFics();

            return new Dictionary<string, Fic>(sd);
        }

        public static void StoreFics(Dictionary<string, Fic> fics)
        {
            Liter.StoreFics(fics);
            //SerializableDictionary<string, Fic> sd = new SerializableDictionary<string, Fic>(fics);
            //XmlOperator.StoreWorks(sd);
        }

        public static T LoadGeneric<T>(string filepath)
        {
            return XmlOperator.DeserializeFile<T>(filepath);
        }

        public static void StoreGeneric<T>(T obj, string filepath)
        {
            XmlOperator.Serialize(obj, filepath);
        }

        public static List<string> GetLibFolders()
        {
            return Liter.GetLib();
        }

        public static void AddLibraryFolder(string folderpath)
        {
            Liter.AddLib(folderpath);
        }

        internal static void StoreLibHashes(Dictionary<string, string> fileHashes)
        {
            Liter.StoreHashes(fileHashes);
        }

        public static void StoreKindle(Dictionary<string, string> hashes)
        {
            Liter.StoreKindleHashes(hashes);
        }

        public static Dictionary<string, string> LoadKindle()
        {
            return Liter.LoadKindleHashes();
        }
    }
}
