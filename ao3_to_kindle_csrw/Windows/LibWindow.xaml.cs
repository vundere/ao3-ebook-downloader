using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Input;
using Config.Net;

namespace AO3EbookDownloader
{
    /// <summary>
    /// Interaction logic for LibWindow.xaml
    /// </summary>
    public partial class LibWindow : Window
    {
        #region Fields

        private bool kindleDisplayed = false;



        public Dictionary<string, Fic> ficList;
        private Dictionary<string, string> kindleFiles;
        private Dictionary<string, Fic> currentDisplay;
        private Dictionary<string, Fic> currentOnHold;  // Var to temporarily hold the "current" library while the kindle files are being displayed.

        private List<string> kindleIds;
        
        #endregion Fields

        #region Constructor
        public LibWindow()
        {
            // HRT.XmlToLdb();
            // GenXml();
            LibInit();
            KindleInit();

            // TODO load in the background...
            LoadFicsToCache();
            kindleFiles = KindleLibrarian.GetKindleList();

            InitializeComponent();
            SetDisplayContent(ficList);

        }

        private void KindleInit()
        {
            if (KindleLibrarian.DetectKindle())
            {

            }

            else
            {

            }
        }

        #endregion Constructor

        #region Methods


        #region Data
        private void GenXml()
        {
            string[] xmlFiles = { Constants.LibHashList, Constants.KindleList, Constants.LibRef, Constants.WorksRef };
            foreach (string xmlFile in xmlFiles)
            {
                string dirpath = Path.GetDirectoryName(xmlFile);
                var di = Directory.CreateDirectory(dirpath);
                if (!File.Exists(xmlFile))
                {
                    using (File.Create(xmlFile)) { };
                }
            }
        }


        private void LibInit()
        {
            List<string> folders = new List<string>();
            Dictionary<string, string> fileHashes = new Dictionary<string, string>();
            var progressWindow = new ProgressWindow();

            Task.Factory.StartNew(() =>
           {
               folders = MiddleDude.GetLibFolders();

               if (folders.Count < 1)
               {
                   using (System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog())
                   {
                       dialog.Description = "Select Folder...";
                       if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                       {
                           string initFolder = dialog.SelectedPath;
                           MiddleDude.AddLibraryFolder(initFolder);
                       }
                   }
               }
           }).ContinueWith(x =>
           {
               progressWindow.StopProgress();
               progressWindow.ChangeText("Locating library contents...");

               string[] allFiles = FileTools.Walker(folders);

               progressWindow.SetProgressBarMax(allFiles.Length);
               progressWindow.StartProgress();
               progressWindow.ChangeText("Processing library files...");

               foreach (string file in allFiles)
               {
                   string ext = Path.GetExtension(file);
                   if (Constants.AllowedExt.Contains(ext.Remove(0,1)))
                   {
                       string workHash = WorkIdent.Hash(file);
                       fileHashes[workHash] = file;
                   }
                   progressWindow.UpdateProgressBar();
               }
               progressWindow.StopProgress();
           }).ContinueWith(x =>
          {
              MiddleDude.StoreLibHashes(fileHashes);
          });

            progressWindow.Close();

            Console.WriteLine("LibInit complete!");
            //using (var writer = new FileStream(Constants.LibHashList, FileMode.Create))
            //{
            //    XmlSerializer ser = new XmlSerializer(typeof(SerializableDictionary<string, string>), new XmlRootAttribute("root"));
            //    ser.Serialize(writer, fileHashes);
            //}

            //XmlOperator.Serialize(fileHashes, Constants.LibHashList);

        }

        private Dictionary<string, string> GetKindleList()
        {
            // If Kindle is not connected, it will use the list of IDs stored from the last time it was.
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

        private void LoadFicsToCache()
        {
            Dictionary<string, Fic> ficListInternal;
            try
            {
                ficListInternal = MiddleDude.GetFics();
            }
            catch (Exception)
            {
                ficListInternal = new Dictionary<string, Fic>();
            }

            ficList = ficListInternal;
        }

        private static SerializableDictionary<string, string> ImportHashes(string filepath)  // Allows for importing a list of ID : [hashes] file. Designed to import XML for now.
        {
            return XmlOperator.DeserializeFile<SerializableDictionary<string, string>>(filepath);
        }

        private List<string> OnKindle(Dictionary<string, string[]> matches)
        {
            List<string> res = new List<string>();

            foreach (KeyValuePair<string, string[]> pair in matches)
            {
                res.Add(pair.Key);
            }

            return res;
        }

        private Dictionary<string, string[]> FindFilesByHash(SerializableDictionary<string, string> hashOne, SerializableDictionary<string, string> hashTwo)
        {
            /* 
             * This method is intended to find matching file hashes and return the matches.
            */
            Dictionary<string, string[]> res = new Dictionary<string, string[]>();
            foreach (KeyValuePair<string, string> pair in hashOne)
            {
                try
                {
                    string kindleFile = hashTwo[pair.Key];
                    string[] files = { pair.Value, kindleFile };
                    res[pair.Key] = files;
                }
                catch (KeyNotFoundException)
                {
                    continue;
                }
            }

            return res;

        }

        private void MatchKindleObjectsFromFile()
        {
            using (System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog())
            {
                dialog.Title = "Select File...";
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string selected = dialog.FileName;
                    try
                    {
                        List<string> kindleIds = new List<string>();
                        SerializableDictionary<string, string> tempDisp = XmlOperator.DeserializeFile<SerializableDictionary<string, string>>(selected);
                        if (kindleFiles != null)
                        {
                            foreach (KeyValuePair<string, string> pair in tempDisp)
                            {
                                try
                                {
                                    if (kindleFiles.ContainsKey(pair.Value))
                                    {
                                        kindleIds.Add(pair.Key);
                                    }
                                }
                                catch (Exception)
                                {
                                    throw;
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }
        }

        #endregion Data

        #region UI

        private void SortByFolder()
        {

        }

        private void SetDisplayContent(Dictionary<string, Fic> objects)
        {
            currentDisplay = objects;
            Resources["ficList"] = currentDisplay;
            this.Dispatcher.Invoke(new Action(() =>
           {
               TextBlockItemCount.Text = objects.Count.ToString();
           }));
        }

        #endregion UI
        private Fic FicFromTb(TextBlock tb)
        {
            var fic_id = tb.Tag.ToString();
            Fic current = currentDisplay[fic_id];

            return current;
        }

        

        #endregion Methods

        #region Events

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //var mainWin = new MainWindow();
            //mainWin.Show();
        }

        private void AuthorLoad(object sender, RoutedEventArgs e)
        {
            TextBlock tb = (TextBlock)sender;
            var current = FicFromTb(tb);
            foreach (string author in current.Author)
            {
                string authurl = Constants.BaseUrl + author.Split(':')[1].TrimStart(' ');
                string authornameonly = author.Split(':')[0];

                var h = new Hyperlink
                {
                    NavigateUri = new Uri(authurl)
                };
                h.Inlines.Add(authornameonly);
                h.RequestNavigate += Hyperlink_RequestNavigate;
                tb.Inlines.Add(h);
                tb.Inlines.Add(new TextBlock()
                {
                    Text = " "
                });
            }
        }

        private void TagsLoad(object sender, RoutedEventArgs e)
        {
            TextBlock tb = (TextBlock)sender;
            var current = FicFromTb(tb);
            var tags = current.AdditionalTags;

            if (tags != null)
            {
                string tagString = String.Join(", ", tags.Select(x => System.Net.WebUtility.HtmlDecode(x)));

                tb.Text = tagString;
            }

            

        }

        private void RelLoad(object sender, RoutedEventArgs e)
        {
            TextBlock tb = (TextBlock)sender;
            var current = FicFromTb(tb);
            var rel = current.Relationship;

            if (rel != null )
            {
                string relString = String.Join(", ", rel.Select(x => System.Net.WebUtility.HtmlDecode(x)));

                tb.Text = relString;
            }
            
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            _ = System.Diagnostics.Process.Start(e.Uri.ToString());
        }

        private void Summary_Loaded(object sender, RoutedEventArgs e)
        {
            TextBlock tb = (TextBlock)sender;
            var current = FicFromTb(tb);
            var summary = current.Summary;
            if (summary != null)
            {
                if (summary.Length > 500)
                {
                    int finalSpace = summary.IndexOf(" ", 190);
                    string shortenedSummary = summary.Substring(0, finalSpace);
                    tb.Text = shortenedSummary + "...\n";

                    var h = new Hyperlink();
                    h.Inlines.Add("See more...");
                    h.Tag = current.ID;
                    h.Click += SummarySeeMore;
                    tb.Inlines.Add(h);
                }
                else
                {
                    tb.Text = summary;
                }
            }
            

        }

        private void SummarySeeMore(object sender, RoutedEventArgs e)
        {
            var parent = (sender as Hyperlink).Parent;
            string id = (sender as Hyperlink).Tag.ToString();
            string fullSummary = ficList[id].Summary;

            var tb = (parent as TextBlock);
            tb.Inlines.Remove((sender as Hyperlink));
            tb.Text = fullSummary;
            var h = new Hyperlink();
            h.Inlines.Add("\nSee less...");
            h.Click += SummarySeeLess;
            tb.Inlines.Add(h);
        }

        private void SummarySeeLess(object sender, RoutedEventArgs e)
        {
            var parent = (sender as Hyperlink).Parent;
            var tb = (parent as TextBlock);

            Summary_Loaded(tb, e);
        }

        private void ButtonLoadXml_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog())
            {
                dialog.Title = "Select File...";
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string selected = dialog.FileName;
                    try
                    {
                        SerializableDictionary<string, Fic> tempDisp = XmlOperator.DeserializeFile<SerializableDictionary<string, Fic>>(selected);
                        // Resources["ficList"] = tempDisp;

                        Dictionary<string, Fic> actualDict = new Dictionary<string, Fic>(tempDisp);
                        //ficList = tempDisp;
                        //Resources["ficList"] = ficList;
                        SetDisplayContent(actualDict);
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }
        }


        #endregion Events

        private void ButtonSaveXml_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ButtonKindleToggle_Click(object sender, RoutedEventArgs e)
        {
            if (!kindleDisplayed)
            {
                kindleDisplayed = true;
                currentOnHold = currentDisplay;

                this.Dispatcher.Invoke(new Action(() =>
                {
                    this.Cursor = Cursors.Wait;
                }));

                SetDisplayContent(KindleLibrarian.KindleObjects(currentDisplay));

                this.Dispatcher.Invoke(new Action(() =>
                {
                    this.Cursor = Cursors.Arrow;
                }));

                buttonKindleToggle.Background = ExTools.ConvertColorFromHexString(Constants.GreenClr);
            }
            else
            {
                kindleDisplayed = false;
                SetDisplayContent(currentOnHold);
                currentOnHold = null;
                buttonKindleToggle.Background = ExTools.ConvertColorFromHexString(Constants.RedClr);
            }
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            Grid gd = (Grid)sender;

            var fic_id = gd.Tag.ToString();
            if (kindleIds.Contains(fic_id))
            {
               
                gd.Background = ExTools.ConvertColorFromHexString(Constants.GreenClr);
            }
        }
    }
}
