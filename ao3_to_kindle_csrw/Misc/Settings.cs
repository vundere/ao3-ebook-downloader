using System;
using Config.Net;

namespace AO3EbookDownloader
{
    public interface Settings
    {
        [Option(DefaultValue = "")]
        string DownloadLocation { get; set; }
        [Option(DefaultValue = "")]
        string DevicePath { get; set; }
        [Option(DefaultValue = 5)]
        int DownloadMaxAttempts { get; set; }
        [Option(DefaultValue = 0.5)]
        double DownloadAttemptCooldown { get; set; }
        [Option(DefaultValue = 2.0)]
        double DownloadAttemptExponential { get; set; }
        //Checkboxes
        [Option(DefaultValue = false)]
        Boolean GetEpub { get; set; }
        [Option(DefaultValue = false)]
        Boolean GetMobi { get; set; }
        [Option(DefaultValue = false)]
        Boolean GetPdf { get; set; }
        [Option(DefaultValue = false)]
        Boolean GetHtml { get; set; }
        [Option(DefaultValue = false)]
        Boolean FormatFolders { get; set; }
    }
}
