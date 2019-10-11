using Config.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AO3EbookDownloader
{
    class Kindle
    {
        public Dictionary<string, string> Hashes;
        public Dictionary<string, string> kindleFiles;
        public List<string> Works;
        public Dictionary<string, Fic> FicObjects;

        public string DevicePath;


        public void Init()
        {
            try
            {
                Hashes = HashList();
            }
            catch (Exception)
            {
                Hashes = new Dictionary<string, string>();
            }

            try
            {
                Works = null;
            }
            catch (Exception)
            {
                Works = new List<string>();
            }

            try
            {
                FicObjects = Objects();
            }
            catch (Exception)
            {
                FicObjects = new Dictionary<string, Fic>();
            }



        }

        #region Non-Static Methods
        // Is there another word for non-static?

        private string[] WorkFiles(string workId)
        {
            string workFile = kindleFiles[workId];
            string companionDir = workFile.Replace(".mobi", ".sdr");

            string[] res = { workFile, companionDir };

            return res;
        }

        private void DeleteFromKindle(string workId)
        {
            string fileToDelete;
            try
            {
                fileToDelete = kindleFiles[workId];
            }
            catch (KeyNotFoundException)
            {
                fileToDelete = null;
            }

            if (!String.IsNullOrEmpty(fileToDelete))
            {
                File.Delete(fileToDelete);
            }
        }

        public void SetWorks(Dictionary<string, Fic> masterWorks)
        {
            if (Works != null)
            {

            }
        }

        public Dictionary<string, Fic> Objects()
        {
            // TODO: Optimize, prevent from blocking UI thread
            Dictionary<string, Fic> res = new Dictionary<string, Fic>();

            List<string> matchedHahes = FileTools.MatchHashes(Hashes, FicObjects);

            foreach (string matchedID in matchedHahes)
            {
                res[matchedID] = FicObjects[matchedID];
            }

            return res;
        }

        public Dictionary<string, string> HashList()
        {
            /* If Kindle is not connected, it will use the list of IDs stored from the last time it was.
             * Returns a dictionary in the format:
             *      Dictionary
             *      {
             *          MD5 hash: Filepath
             *      }
             */
            Dictionary<string, string> fileHashes = new Dictionary<string, string>();
            var progressWindow = new ProgressWindow();
            progressWindow.Show();
            progressWindow.StopProgress();

            Task.Factory.StartNew(() =>
            {
                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    string driveName = drive.IsReady ? drive.VolumeLabel : null;  // Makes sure we only try to query mounted drives
                    if (driveName != null && driveName.Contains("Kindle"))  // TODO more robust Kindle detection
                    {
                        // Kindle is connected, get current library


                        progressWindow.ChangeText("Locating Kindle contents...");


                        string[] kindleFiles = FileTools.Walker(new List<string> { drive.Name });
                        progressWindow.SetProgressBarMax(kindleFiles.Length);
                        progressWindow.StartProgress();
                        progressWindow.ChangeText("Processing Kindle files...");

                        foreach (string file in kindleFiles)
                        {
                            string ext = Path.GetExtension(file);
                            if (ext.StartsWith("."))
                            {
                                ext = ext.Substring(1);
                            }
                            if (Constants.AllowedExt.Contains(ext))
                            {
                                string workHash = WorkIdent.Hash(file);
                                fileHashes[workHash] = file;
                            }

                            progressWindow.UpdateProgressBar();
                        }

                        MiddleDude.StoreKindle(fileHashes);
                    }
                }
            });


            progressWindow.Close();
            Console.WriteLine("Kindle fetch complete!");
            try
            {
                fileHashes = MiddleDude.LoadKindle();
                return fileHashes;
            }
            catch (InvalidOperationException)
            {
                return fileHashes;
            }
        }

        #endregion Non-Static Methods

        #region Static

        public static Settings userSettings = new ConfigurationBuilder<Settings>().UseIniFile(Constants.SettingsPath).Build();

        public bool Detect()
        {
            // TODO move this out somewhere?
            DriveInfo[] drives = DriveInfo.GetDrives();
            for (int i = 0; i < drives.Count(); i++)
            {
                try
                {
                    if (drives[i].VolumeLabel == "Kindle" && (userSettings.DevicePath == "" || drives[i].Name.First() != userSettings.DevicePath.First()))
                    {
                        DevicePath = drives[i].Name;
                        return true;
                    }

                }
                catch (IOException)
                {
                    continue;
                }
            }
            return false;
        }


        #endregion Static
    }
}
