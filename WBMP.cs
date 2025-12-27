using System.IO;
using System.Numerics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RIKA_TEXTURER
{
    public static class WBMP
    {
        public static WriteableBitmap Create(int texSize)
        {
            return new WriteableBitmap(
                texSize,
                texSize,
                96, 96,
                PixelFormats.Bgra32,
                null);
        }

        public static void DrawCircle(WriteableBitmap bitmap, Vector2 center, double radius, Color color)
        {
            if (bitmap == null) return;

            int x0 = (int)center.X;
            int y0 = (int)center.Y;
            int r = (int)radius;

            bitmap.Lock();

            try
            {
                unsafe
                {
                    byte* buffer = (byte*)bitmap.BackBuffer;
                    int stride = bitmap.BackBufferStride;
                    int bytesPerPixel = bitmap.Format.BitsPerPixel / 8;

                    int x = r;
                    int y = 0;
                    int d = 3 - 2 * r;

                    while (y <= x)
                    {
                        DrawCirclePoints(buffer, stride, bytesPerPixel, x0, y0, x, y, color);
                        y++;

                        if (d > 0)
                        {
                            x--;
                            d = d + 4 * (y - x) + 10;
                        }
                        else
                        {
                            d = d + 4 * y + 6;
                        }
                    }
                }
            }
            finally
            {
                bitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
                bitmap.Unlock();
            }
        }

        private static unsafe void DrawCirclePoints(
            byte* buffer, int stride, int bytesPerPixel,
            int x0, int y0, int x, int y, Color color)
        {
            SetPixel(buffer, stride, bytesPerPixel, x0 + x, y0 + y, color);
            SetPixel(buffer, stride, bytesPerPixel, x0 - x, y0 + y, color);
            SetPixel(buffer, stride, bytesPerPixel, x0 + x, y0 - y, color);
            SetPixel(buffer, stride, bytesPerPixel, x0 - x, y0 - y, color);
            SetPixel(buffer, stride, bytesPerPixel, x0 + y, y0 + x, color);
            SetPixel(buffer, stride, bytesPerPixel, x0 - y, y0 + x, color);
            SetPixel(buffer, stride, bytesPerPixel, x0 + y, y0 - x, color);
            SetPixel(buffer, stride, bytesPerPixel, x0 - y, y0 - x, color);
        }

        private static unsafe void SetPixel(byte* buffer, int stride, int bytesPerPixel, int x, int y, Color color)
        {
            int index = y * stride + x * bytesPerPixel;

            buffer[index] = color.B;
            buffer[index + 1] = color.G;
            buffer[index + 2] = color.R;
            buffer[index + 3] = color.A;
        }

        public static void FillCircle(WriteableBitmap bitmap, Vector2 center, double radius, Color color)
        {
            int x0 = (int)center.X;
            int y0 = (int)center.Y;
            int r = (int)radius;

            bitmap.Lock();

            try
            {
                unsafe
                {
                    byte* buffer = (byte*)bitmap.BackBuffer;
                    int stride = bitmap.BackBufferStride;
                    int bytesPerPixel = 4;

                    int left = Math.Max(0, x0 - r);
                    int right = Math.Min(bitmap.PixelWidth - 1, x0 + r);
                    int top = Math.Max(0, y0 - r);
                    int bottom = Math.Min(bitmap.PixelHeight - 1, y0 + r);

                    int rSquared = r * r;

                    for (int y = top; y <= bottom; y++)
                    {
                        int dy = y - y0;
                        int dySquared = dy * dy;
                        int xLimit = (int)Math.Sqrt(rSquared - dySquared);
                        int xStart = Math.Max(left, x0 - xLimit);
                        int xEnd = Math.Min(right, x0 + xLimit);

                        for (int x = xStart; x <= xEnd; x++)
                        {
                            int index = y * stride + x * bytesPerPixel;
                            buffer[index] = color.B;
                            buffer[index + 1] = color.G;
                            buffer[index + 2] = color.R;
                            buffer[index + 3] = color.A;
                        }
                    }
                }
            }
            finally
            {
                bitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
                bitmap.Unlock();
            }
        }

        public static void SaveToPng(WriteableBitmap wbmp, string filePath)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(wbmp));
                encoder.Save(fileStream);
            }
        }

        public static BitmapImage ConvertToBitmapImage(WriteableBitmap writableBitmap)
        {
            BitmapImage bitmapImage = new BitmapImage();
            PngBitmapEncoder encoder = new PngBitmapEncoder();

            using (var memoryStream = new MemoryStream())
            {
                encoder.Frames.Add(BitmapFrame.Create(writableBitmap));
                encoder.Save(memoryStream);

                memoryStream.Seek(0, SeekOrigin.Begin);

                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
            }

            return bitmapImage;
        }
    }
}