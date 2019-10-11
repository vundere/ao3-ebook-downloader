using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AO3EbookDownloader
{
    internal static class Xpaths
    {
        // Xpath values
        public static string Title = "//h2[@class=\"title heading\"]"; // //text() can be appended to select text directly

        public static string Words = "//dd[@class=\"words\"]"; // //text() can be appended to select text directly

        public static string Download = "//a[text() = \"{0}\"]";

        public static string Chapters = "//dd[@class=\"chapters\"]";

        public static string Author = "//a[@rel=\"author\"]";

        public static string Published = "//dd[@class=\"published\"]";

        public static string Updated = "//dd[@class=\"updated\"]";

        public static string Rating = "//dd[contains(concat(' ', @class, ' '), ' rating ')]//li/a";

        public static string Category = "//dd[contains(concat(' ', @class, ' '), ' category ')]//li/a";

        public static string Fandom = "//dd[contains(concat(' ', @class, ' '), ' fandom ')]//li/a";

        public static string Relationship = "//dd[contains(concat(' ', @class, ' '), ' relationship ')]//li/a";

        public static string Characters = "//dd[contains(concat(' ', @class, ' '), ' character ')]//li/a";

        public static string Tags = "//dd[contains(concat(' ', @class, ' '), ' freeform ')]//li/a";

        public static string Summary = "//div[contains(concat(' ', @class, ' '), ' summary ')]//p";
    }
}
