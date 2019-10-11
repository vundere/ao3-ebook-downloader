using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace AO3EbookDownloader
{
    class ExTools
    {
        public static SolidColorBrush ConvertColorFromHexString(string colorStr)
        {
            //Target hex string
            colorStr = colorStr.Replace("#", string.Empty);
            // from #RRGGBB string
            var r = (byte)System.Convert.ToUInt32(colorStr.Substring(0, 2), 16);
            var g = (byte)System.Convert.ToUInt32(colorStr.Substring(2, 2), 16);
            var b = (byte)System.Convert.ToUInt32(colorStr.Substring(4, 2), 16);
            //get the color
            Color color = Color.FromArgb(255, r, g, b);
            // create the solidColorbrush
            var myBrush = new SolidColorBrush(color);
            return myBrush;
        }

    }
}
