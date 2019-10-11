using System;
using System.Windows;
using System.Windows.Forms;

namespace AO3EbookDownloader
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class ProgressWindow : Window
    {
        public ProgressWindow()
        {
            InitializeComponent();
        }

        public void StopProgress()
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                progressBar.IsIndeterminate = true;
            }));
        }

        public void StartProgress()
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                progressBar.IsIndeterminate = false;
            }));
        }

        public void HoldProgress()
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                if (progressBar.IsIndeterminate)
                    progressBar.IsIndeterminate = false;
                else
                    progressBar.IsIndeterminate = true;
            }));
        }

        public void SetProgressBarMax(int maxValue)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                progressBar.Maximum = maxValue;
            }));
        }

        public void UpdateProgressBar()
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                progressBar.Value++;
            }));
        }

        public void ChangeText(string newMessage)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                labelProgressBar.Content = newMessage;
            }));
        }
    }
}
