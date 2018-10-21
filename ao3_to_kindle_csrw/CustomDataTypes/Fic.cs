using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AO3EbookDownloader
{
    internal class Fic
    {
        public String Title { get; set; }
        public int Length { get; set; }
        public Dictionary<string, string> Downloads { get; set; }
    }
}
