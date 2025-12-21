using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RIKA_TEXTURER
{
    public static class Texturer
    {
        public static BitmapImage _img;
        public static Obj _obj;

        public static void Do(int texSize)
        {
            var bitmap = new WriteableBitmap(texSize, texSize, 96, 96, PixelFormats.Bgra32, null);

        }
    }
}
