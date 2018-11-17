using System;
using System.Collections.Generic;

namespace AO3EbookDownloader
{
    internal class Fic
    {
        public String Title { get; set; }
        public int Length { get; set; }
        public Dictionary<string, string> Downloads { get; set; }
    }
}
