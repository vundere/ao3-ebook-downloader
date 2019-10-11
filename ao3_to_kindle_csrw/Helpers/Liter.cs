using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;

namespace AO3EbookDownloader
{
    class Liter
    {
        public static void StoreFic(Fic fic)
        {
            using(var db = new LiteDatabase(Constants.LiteDbPath))
            {
                var store = db.GetCollection<Fic>("Works");

                store.Upsert(fic);
            }
        }

        public static void StoreFics(Dictionary<string, Fic> fDict)
        {
            using (var db = new LiteDatabase(Constants.LiteDbPath))
            {
                var store = db.GetCollection<Fic>("Works");

                foreach (KeyValuePair<string, Fic> pair in fDict)
                {
                    store.Upsert(pair.Value);
                }
            }
        }

        public static void StoreList(List<string> l)
        {
            using (var db = new LiteDatabase(Constants.LiteDbPath))
            {
                var store = db.GetCollection<string>("KindleFiles");

                foreach (string substr in l)
                {
                    store.Upsert(substr);
                }
            }
        }


        public static Fic GetFic(string idString)
        {
            using(var db = new LiteDatabase(Constants.LiteDbPath))
            {
                var store = db.GetCollection<Fic>("Works");

                var result = store.FindOne(Query.EQ("ID", idString));

                return result;
            }
        }

        public static Dictionary<string, Fic> GetFics()
        {
            using (var db = new LiteDatabase(Constants.LiteDbPath))
            {
                var store = db.GetCollection<Fic>("Works");

                var results = store.Find(Query.All());

                Dictionary<string, Fic> res = new Dictionary<string, Fic>();
                foreach (Fic fic in results)
                {
                    res[fic.ID] = fic;
                }
                return res;
            }
        }

        internal static void StoreHashes(Dictionary<string, string> hashes)
        {
            if (hashes.Keys.Count > 0)
            {
                using (var db = new LiteDatabase(Constants.LiteDbPath))
                {
                    var store = db.GetCollection("Hashes");

                    foreach (KeyValuePair<string, string> pair in hashes)
                    {
                        store.Upsert(new BsonDocument { ["_id"] = pair.Key, ["Value"] = pair.Value });
                    }
                }
            }
            
        }

        public static void StoreKindleIds(List<string> kindleIds)
        {
            using (var db = new LiteDatabase(Constants.LiteDbPath))
            {
                var store = db.GetCollection("KindleId");

                foreach (string kindleId in kindleIds)
                {
                    store.Upsert(new BsonDocument { ["_id"] = kindleId });
                }
            }
        }

        public List<string> LoadKindleIds()
        {
            List<string> res = new List<string>();

            using (var db = new LiteDatabase(Constants.LiteDbPath))
            {
                var store = db.GetCollection("KindleIds");

                var result = store.FindAll();

                foreach (BsonDocument bd in result)
                {
                    res.Add(bd["_id"]);
                }
            }

            return res;
        }

        public static void StoreKindleHashes(Dictionary<string, string> inDict)
        {
            using (var db = new LiteDatabase(Constants.LiteDbPath))
            {
                var store = db.GetCollection("KindleHashes");

                foreach (KeyValuePair<string, string> pair in inDict)
                {
                    store.Upsert(new BsonDocument { ["_id"] = pair.Key, ["Value"] = pair.Value });
                }
            }
        }

        public static Dictionary<string, string> LoadKindleHashes()
        {
            Dictionary<string, string> res = new Dictionary<string, string>();

            using (var db = new LiteDatabase(Constants.LiteDbPath))
            {
                var store = db.GetCollection("KindleHashes");

                var result = store.FindAll();


                foreach (BsonDocument bd in result)
                {
                    res[bd["_id"]] = bd["Value"];
                }
            }
            return res;
        }

        public static void AddLib(string fpath)
        {
            using (var db = new LiteDatabase(Constants.LiteDbPath))
            {
                var store = db.GetCollection("Library");

                store.Upsert(new BsonDocument { ["Folder"] = fpath });
            }
        }

        public static List<string> GetLib()
        {
            List<string> res = new List<string>();
            using (var db = new LiteDatabase(Constants.LiteDbPath))
            {
                var store = db.GetCollection("Library");

                var results = store.FindAll();

                foreach (BsonDocument result in results)
                {
                    res.Add(result["Folder"]);
                }
            }
            return res;
        }

    }
}
