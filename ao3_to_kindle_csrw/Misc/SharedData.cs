using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Config.Net;

namespace AO3EbookDownloader
{
    class SharedData
    {
        /*
         * This is a class that contains data meant to be accessed multiple windows
         */

        public static Settings userSettings { get; set; }
        public static Kindle kindle { get; set; }
    }
}
