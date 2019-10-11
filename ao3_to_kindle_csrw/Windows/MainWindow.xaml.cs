using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Config.Net;
using System.Threading;
using System.Reflection;
using System.Diagnostics;

namespace AO3EbookDownloader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields

        private Settings userSettings;

        private List<WebClient> pendingDownloads; 

        private List<string> downloadedFiles = new List<string>();

        private Boolean activeDownloads = false;

        private Boolean userCancel = false;

        private LibWindow libWin;

        #endregion Fields

        #region Constructor

        public MainWindow()
        {
            SharedData.userSettings = new ConfigurationBuilder<Settings>().UseIniFile(Constants.SettingsPath).Build();
            userSettings = SharedData.userSettings;

            InitializeSettings();
            InitializeComponent();
            DataContext = userSettings;

            pasteBox.Text = Constants.PasteBoxDefText;
            pasteBox.Foreground = new SolidColorBrush(Colors.DarkGray);

            ServicePointManager.DefaultConnectionLimit = 50;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            labelVersion.Content = "v " + Assembly.GetEntryAssembly().GetName().Version;
        }

        #endregion Constructor

        #region Methods

        private string DownloadEbook(String downloadUrl)
        {
            if (!CreateFolder(userSettings.DownloadLocation))
            {
                return null;
            }

            int attempts = 0;
            bool fileDownloaded = false;
            string filename;
            Uri uri = new Uri(downloadUrl);
            string work_id = uri.Segments[2];
            work_id = work_id.Remove(work_id.Length - 1);
            string downloadUrlClean = uri.GetLeftPart(UriPartial.Path);
            filename = FileTools.ToAllowedFileName(uri.Segments.Last());
            filename = $"{work_id}-{filename}";
            filename = $"{userSettings.DownloadLocation}/{filename}";

            if (File.Exists(filename))
            {
                // TODO popup window to allow for replacing existing file? checkbox to enable/disable feature?
                Log("This file already exists, skipping...");
                return null;
            }

            do
            {
                var doneEvent = new AutoResetEvent(false);  // TODO dispose
                using (var w = new WebClient())
                {
                    

                    w.DownloadFileCompleted += (s, e) =>
                    {
                        AttemptCooldown(attempts);
                        attempts++;

                        if (!e.Cancelled && e.Error == null)
                        {
                            fileDownloaded = true;
                        }
                        else if (!e.Cancelled && e.Error != null)
                        {
                            if (attempts < userSettings.DownloadMaxAttempts)
                            {
                                Log($"Failed to download {downloadUrl}, retrying...");
                            }
                            else
                            {
                                Log($"Failed to download {downloadUrl}, max retries reached.");
                            }
                        }
                        doneEvent.Set();
                    };
                        

                    lock (this.pendingDownloads)
                    {
                        if (this.userCancel)
                        {
                            return null;
                        }
                        this.pendingDownloads.Add(w);
                        w.DownloadFileAsync(new Uri(downloadUrl), filename);
                    }
                    doneEvent.WaitOne();
                    lock (this.pendingDownloads)
                    {
                        downloadedFiles.Add(filename);
                        this.pendingDownloads.Remove(w);
                    }
                    IncrementProgress(labelProgressDownloaded);
                    Log($"Downloaded {filename}...");
                }
            }
            while (!fileDownloaded && attempts < userSettings.DownloadMaxAttempts);

            return filename;
        }

        private List<string> getSelectedFormats()
        {
            List<string> selFormats = new List<string>();

            // Look into a more efficient way of accomplishing this
            if (userSettings.GetAzw3)
            {
                selFormats.Add("AZW3");
            }
            if (userSettings.GetEpub)
            {
                selFormats.Add("EPUB");
            }
            if (userSettings.GetMobi)
            {
                selFormats.Add("MOBI");
            }
            if (userSettings.GetPdf)
            {
                selFormats.Add("PDF");
            }
            if (userSettings.GetHtml)
            {
                selFormats.Add("HTML");
            }

            return selFormats;
        }
    
        private Fic GetFic(String url)
        {
            Fic fic = new Fic();
            url += "?view_adult=true";

            if (new Uri(url).Host != new Uri(Constants.BaseUrl).Host)
            {
                Log($"{url} is from an unsupported domain, this application only supports pages from AO3.");
                return null;
            }
            if (url.Contains("series"))
            {
                Log($"{url} is a series, this application currently does not support downloading entire series. Skipping...");
                return null;
            }

            Log("Fetching " + url);

            try
            {
                using (WebClient w = new WebClient())
                {
                    w.Encoding = System.Text.Encoding.UTF8;
                    if (this.userCancel)
                    {
                        return null;
                    }

                    string htmlCode;
                    try
                    {
                        htmlCode = w.DownloadString(url);
                    }
                    catch
                    {
                        Log($"Failed to get data for {url}");
                        return null;
                    }

                    try
                    {
                        fic = AO3LinkHelper.GetFicData(htmlCode);
                    }
                    catch
                    {
                        htmlCode = w.DownloadString(AO3LinkHelper.ProceedUrl(url));
                        fic = AO3LinkHelper.GetFicData(htmlCode);
                    }
                }
                fic.Url = url;
                return fic;
            }
            catch (Exception)
            {
                Log($"Unable to get download links for {url}");
                return null;
            }
        }

        private void MoveToDevice(String fPath)
        {
            string fileName = Path.GetFileName(fPath);
            string sourcePath = Path.GetDirectoryName(fPath);
            string targetPath = userSettings.DevicePath;

            string sourceFile = Path.Combine(sourcePath, fileName);
            string destFile = Path.Combine(targetPath, fileName);

            CreateFolder(targetPath);

            if (File.Exists(destFile))
            {
                Log($"{fileName} already exists on device, skipping...");
                return;
            }

            File.Copy(sourceFile, destFile, true);
            IncrementProgress(labelProgressCopied);
            Log($"{fileName} moved to device.");
        }

        private void UpdateControls(Boolean state)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                if (state)
                {
                    logOutput.Document.Blocks.Clear();
                    buttonStart.IsEnabled = false;
                    buttonCancel.IsEnabled = true;
                    buttonMove.IsEnabled = false;
                    dlBrowse.IsEnabled = false;
                    deviceBrowse.IsEnabled = false;
                    checkAzw3.IsEnabled = false;
                    checkEpub.IsEnabled = false;
                    checkMobi.IsEnabled = false;
                    checkPdf.IsEnabled = false;
                    checkHtml.IsEnabled = false;
                    saveLocation.IsReadOnly = true;
                    deviceLocation.IsReadOnly = true;
                }
                else
                {
                    buttonStart.IsEnabled = true;
                    buttonCancel.IsEnabled = false;
                    dlBrowse.IsEnabled = true;
                    deviceBrowse.IsEnabled = true;
                    checkAzw3.IsEnabled = true;
                    checkEpub.IsEnabled = true;
                    checkMobi.IsEnabled = true;
                    checkPdf.IsEnabled = true;
                    checkHtml.IsEnabled = true;
                    saveLocation.IsReadOnly = false;
                    deviceLocation.IsReadOnly = false;
                    if (this.downloadedFiles.Count > 0)
                    {
                        buttonMove.IsEnabled = true;
                    }
                }
            }));
            
        }

        public Boolean CreateFolder(String folderPath)
        {
            try
            {
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
                
            }
            catch
            {
                Log($"Error creating directory {folderPath}.");
                return false;
            }
            return true;
        }

        private void AttemptCooldown(int attempts)
        {
            if (userSettings.DownloadMaxAttempts != 0)
            {
                Thread.Sleep((int)( ( Math.Pow(userSettings.DownloadAttemptExponential, attempts)) * userSettings.DownloadAttemptCooldown * 1000 ));
            }
        }

        private void DetectKindle()
        {
            if (Kindle.Detect())
            {

            }

            DriveInfo[] drives = DriveInfo.GetDrives();
            for (int i = 0; i < drives.Count(); i++)
            {
                try
                {
                    if (drives[i].VolumeLabel == "Kindle" && (userSettings.DevicePath == "" || drives[i].Name.First() != userSettings.DevicePath.First()))
                    {
                        if (MessageBox.Show("A Kindle was detected, set device location to Kindle path?", "AO3 eBook Downloader", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.No)
                        {
                            userSettings.DevicePath = drives[i].Name + "documents";
                            this.Dispatcher.Invoke(new Action(() => 
                            {
                                deviceLocation.Text = userSettings.DevicePath;
                            }));
                        }
                        break;
                    }
                        

                }
                catch (IOException)
                {
                    continue;
                }
            }
        }

        private void IncrementProgress(System.Windows.Controls.Label label)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                label.Content = Convert.ToInt32(label.Content) + 1;
            }));
        }

        private void InitializeSettings()
        {
            if(String.IsNullOrEmpty(userSettings.DownloadLocation))
            {
                userSettings.DownloadLocation = Environment.CurrentDirectory + "\\ebooks";
            }

            userSettings = new ConfigurationBuilder<Settings>().UseIniFile(Constants.SettingsPath).Build();
            DataContext = userSettings;
        }

        public void Log(String message)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                // Time
                var textRange = new TextRange(logOutput.Document.ContentEnd, logOutput.Document.ContentEnd)
                {
                    Text = DateTime.Now.ToString("HH:mm:ss") + " "
                };
                textRange.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Gray);
                // Content
                textRange = new TextRange(logOutput.Document.ContentEnd, logOutput.Document.ContentEnd)
                {
                    Text = message
                };
                textRange.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Black);
                logOutput.AppendText(Environment.NewLine);
                logOutput.ScrollToEnd();
            }));
        }

        #endregion Methods

        #region Events

        private void SaveBrowse_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Select Folder...";
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    saveLocation.Text = dialog.SelectedPath;
                }
            }

        }

        private void DeviceBrowse_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Select Folder...";
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    deviceLocation.Text = dialog.SelectedPath;
                }
            }


        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if(pasteBox.Text == Constants.PasteBoxDefText)
            {
                Log("There are no URLs to download!");
                return;
            }

            List<String> urlEntries = pasteBox.Text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            urlEntries = urlEntries.Distinct().ToList();
            

            this.pendingDownloads = new List<WebClient>();
            this.activeDownloads = true;
            UpdateControls(true);

            var fics = new SerializableDictionary<string, Fic>();
            var dlLinks = new List<String>();

            Task.Factory.StartNew(() =>
            {
                foreach (string urlEntry in urlEntries)
                {
                    Fic fic = GetFic(urlEntry);
                    if (fic != null)
                    {
                        fics[fic.ID] = fic;
                        IncrementProgress(labelProgressedFetched);
                    }
                }
            }).ContinueWith(x =>
            {
                // TODO create flag
                //foreach (Fic fic in fics)
                //{
                //    foreach (KeyValuePair<string, string> entry in fic.Downloads)
                //    {
                //        dlLinks.Add(entry.Value);
                //    }

                //}
                //Task[] tasks = new Task[dlLinks.Count];
                //for (int i = 0; i < dlLinks.Count; i++)
                //{
                //    string link = dlLinks[i];
                //    tasks[i] = Task.Factory.StartNew(() => DownloadEbook(link));  // THIS IS IMPORTANT
                //}
                //Task.WaitAll(tasks);  // SO IS THIS
            //}).ContinueWith(x =>
            //{
                // Both download methods are kept for now, for performance testing purposes

                // New method, to hopefully make connecting metadata and file easier
                List<string> formats = getSelectedFormats();
                int totalDl = fics.Count * formats.Count;
                int currentDl = 0;
                Task[] tasks = new Task[totalDl];
                foreach (KeyValuePair<string, Fic> pair in fics)
                {
                    var fic = pair.Value;
                    tasks[currentDl] = Task.Factory.StartNew(() => 
                    {
                        foreach (KeyValuePair<string, string> entry in fic.Downloads)
                        {
                            if (formats.Contains(entry.Key))
                            {

                                string success = DownloadEbook(entry.Value);
                                if (!String.IsNullOrEmpty(success))
                                {
                                    fic.LocalFiles[entry.Key] = success;
                                }
                            }
                        }
                    });
                    currentDl++;
                    
                }
                Task.WaitAll(tasks);

            }).ContinueWith(x => 
            {
                if (this.userCancel)
                {
                    Log("Cancelled by user.");
                }
            }).ContinueWith(x => 
            {
                this.activeDownloads = false;
                Dictionary<string, Fic> allFics = new Dictionary<string, Fic>();
                try
                {
                    allFics = MiddleDude.GetFics();
                    //allFics = XmlOperator.DeserializeFile<SerializableDictionary<string, Fic>>(Constants.WorksRef);
                }
                catch (System.Xml.XmlException)
                {

                }
                if (allFics != null)
                {
                    foreach (KeyValuePair<string, Fic> pair in fics)
                    {
                        if (!allFics.ContainsKey(pair.Key))
                        {
                            allFics[pair.Key] = pair.Value;
                        }
                        else
                        {
                            Fic mergedFic = Fic.Merge(allFics[pair.Key], pair.Value);
                            allFics[pair.Key] = mergedFic;
                        }
                    }
                }
                else
                {
                    allFics = fics;
                }
                MiddleDude.StoreFics(allFics);
                UpdateControls(false);
                Log("Downloads complete!");
            });
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Cancel downloads?", "AO3 eBook Downloader", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes)
            {
                return;
            }

            this.userCancel = true;
            Cursor = Cursors.Wait;
            Log("Stopping...");

            lock (this.pendingDownloads)
            {
                if (this.pendingDownloads.Count == 0)
                {
                    Cursor = Cursors.Arrow;
                    return;
                }
            }

            buttonCancel.IsEnabled = false;

            lock (this.pendingDownloads)
            {
                foreach (WebClient w in this.pendingDownloads)
                {
                    w.CancelAsync();
                }
            }

            Cursor = Cursors.Arrow;

        }

        private void Move_Click(object sender, RoutedEventArgs e)
        {
            // TODO only move selected formats; if html and epub are selected while downloading, and html is deselected before moving, only move epub
            if (Directory.Exists($@"{userSettings.DevicePath.Substring(0, 1)}:\"))
            {
                Task.Factory.StartNew(() =>
                {
                    Task[] tasks = new Task[downloadedFiles.Count];
                    for (int i = 0; i < downloadedFiles.Count; i++)
                    {
                        string fpath = downloadedFiles[i];
                        tasks[i] = Task.Factory.StartNew(() => MoveToDevice(fpath));
                    }
                    Task.WaitAll(tasks);
                }).ContinueWith(x => 
                {
                    // Clear the list of downloaded files, disable the button to copy them
                    // TODO better validation, retry etc.
                    downloadedFiles.Clear();
                    this.Dispatcher.Invoke(new Action(() => 
                    {
                        buttonMove.IsEnabled = false;
                    }));
                    Log("Files copied successfully!");
                });
            }
            else
            {
                Log("Device not found, check if the drive letter has changed.");
            }
        }

        private void ButtonLib_Click(object sender, RoutedEventArgs e)
        {
            if (!activeDownloads)
            {
                //Close();
            }
            if (libWin == null)
            {
                this.Dispatcher.Invoke(new Action(() =>
                {
                    libWin = new LibWindow();
                    libWin.Show();
                    libWin.Closed += new EventHandler(LibWin_Closed);
                }));
            }
            else
            {
                libWin.Activate();
            }
        }

        private void LibWin_Closed(object sender, EventArgs e)
        {
            libWin = null;
        }

        private void PasteBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if(pasteBox.Text == Constants.PasteBoxDefText)
            {
                pasteBox.Text = "";
                pasteBox.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        private void PasteBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if(pasteBox.Text == "")
            {
                pasteBox.Text = Constants.PasteBoxDefText;
                pasteBox.Foreground = new SolidColorBrush(Colors.DarkGray);
            }
            else
            {
                var lines = pasteBox.Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).Where(x => !string.IsNullOrEmpty(x)).ToArray();
                pasteBox.Text = string.Join($"{Environment.NewLine}", lines);
                this.Dispatcher.Invoke(new Action(() =>
                {
                    labelProgressLinks.Content = lines.Length;
                }));
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.activeDownloads)
            {
                if (MessageBox.Show("There are active downloads, are you sure you want to quit?", "AO3 eBook Downloader", MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel) == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                }
            }
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            DetectKindle();
        }

        private void LabelVersion_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Process.Start(Constants.GitHubUrl);
        }


        #endregion Events


    }
}
