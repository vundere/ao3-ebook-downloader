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

        public Settings userSettings = new ConfigurationBuilder<Settings>().UseIniFile(Constants.SettingsPath).Build();

        private List<WebClient> pendingDownloads; 

        private List<string> downloadedFiles = new List<string>();

        private Boolean activeDownloads = false;

        private Boolean userCancel = false;

        #endregion Fields

        #region Constructor

        public MainWindow()
        {
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

        private Boolean DownloadEbook(String downloadUrl)
        {
            if (!CreateFolder(userSettings.DownloadLocation))
            {
                return false;
            }

            int attempts = 0;
            bool fileDownloaded = false;
            string filename;
            Uri uri = new Uri(downloadUrl);
            string downloadUrlClean = uri.GetLeftPart(UriPartial.Path);
            filename = FilenameCleaner.ToAllowedFileName(uri.Segments.Last());
            filename = $"{userSettings.DownloadLocation}/{filename}";

            if (File.Exists(filename))
            {
                // TODO popup window to allow for replacing existing file? checkbox to enable/disable feature?
                Log("This file already exists, skipping...");
                return false;
            }

            do
            {
                var doneEvent = new AutoResetEvent(false);
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
                            return false;
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

            return true;
        }

        private List<string> getSelectedFormats()
        {
            List<string> selFormats = new List<string>();

            // Look into a more efficient way of accomplishing this
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
    
        private Fic getFic(String url)
        {
            Fic fic = new Fic();
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
                fic = DownloadLinkFinder.GetFic(url, getSelectedFormats());
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

        private Boolean CreateFolder(String folderPath)
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
            DriveInfo[] drives = DriveInfo.GetDrives();
            for (int i = 0; i < drives.Count(); i++)
            {
                try
                {
                    if (drives[i].VolumeLabel == "Kindle" && drives[i].Name.First() != userSettings.DevicePath.First())
                        if (MessageBox.Show("A Kindle was detected, set device location to that?", "AO3 eBook Downloader", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.No)
                        {
                            userSettings.DevicePath = drives[i].Name + "documents";
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

        private void Log(String message)
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
            this.Dispatcher.Invoke(new Action(() => 
            {
                labelProgressLinks.Content = urlEntries.Count;
            }));

            this.pendingDownloads = new List<WebClient>();
            this.activeDownloads = true;
            UpdateControls(true);

            var fics = new List<Fic>();
            var dlLinks = new List<String>();

            Task.Factory.StartNew(() =>
            {
                Task[] tasks = new Task[urlEntries.Count];
                for (int i = 0; i < urlEntries.Count; i++)
                {
                    string urlEntry = urlEntries[i];
                    tasks[i] = Task.Factory.StartNew(() => 
                    {
                        Fic fic = getFic(urlEntry);
                        if (fic != null)
                        {
                            fics.Add(fic);
                        }
                    });
                }
                Task.WaitAll(tasks);
            }).ContinueWith(x =>
            {
                foreach (Fic fic in fics)
                {
                    foreach (KeyValuePair<string, string> entry in fic.Downloads)
                    {
                        dlLinks.Add(entry.Value);
                    }
                    
                }
            }).ContinueWith(x =>
            {
                Task[] tasks = new Task[dlLinks.Count];
                for (int i = 0; i < dlLinks.Count; i++)
                {
                    string link = dlLinks[i];
                    tasks[i] = Task.Factory.StartNew(() => DownloadEbook(link));
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
                var lines = pasteBox.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.None).Where(x => !string.IsNullOrEmpty(x)).ToArray();
                pasteBox.Text = string.Join($"{Environment.NewLine}", lines);
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
