using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace RIKA_IMBANIKA_TEXTURER
{
    public static class Rainbow
    {
        public static Color GetRainbowColor(int index, int cycleLength = 150)
        {
            float phase = (index % cycleLength) / (float)cycleLength;

            if (phase < 0) phase += 1.0f;

            float red = (float)(Math.Sin(2 * Math.PI * phase) * 0.5 + 0.5);
            float green = (float)(Math.Sin(2 * Math.PI * phase + Math.PI * 0.666) * 0.5 + 0.5);
            float blue = (float)(Math.Sin(2 * Math.PI * phase + Math.PI * 1.333) * 0.5 + 0.5);

            return Color.FromArgb(
                (byte)255,
                (byte)(red * 255),
                (byte)(green * 255),
                (byte)(blue * 255)
            );
        }

        public static Color GetRainbowColor2(int index, int cycleLength = 150)
        {
            float phase = (index % cycleLength) / (float)cycleLength;

            if (phase < 0) phase += 1.0f;

            float red = (float)(Math.Sin(2 * Math.PI * phase + 0) * 0.5 + 0.5);
            float green = (float)(Math.Sin(2 * Math.PI * phase + 2 * Math.PI / 3) * 0.5 + 0.5);
            float blue = (float)(Math.Sin(2 * Math.PI * phase + 4 * Math.PI / 3) * 0.5 + 0.5);

            return Color.FromArgb(
                (byte)255,
                (byte)(red * 255),
                (byte)(green * 255),
                (byte)(blue * 255)
            );
        }
    }
}
