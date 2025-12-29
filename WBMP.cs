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

        public static void DaemonFiller(WriteableBitmap bitmap, Vector2 center, float radius, ushort island, Color clr)
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
                        int xLimit = (int)MathF.Sqrt(rSquared - dySquared);
                        int xStart = Math.Max(left, x0 - xLimit);
                        int xEnd = Math.Min(right, x0 + xLimit);

                        for (int x = xStart; x <= xEnd; x++)
                        {
                            if (Texturer.GetIsland(x, y) != island)
                                continue;

                            int dx = x - x0;
                            int index = y * stride + x * bytesPerPixel;

                            byte currentB = buffer[index];
                            byte currentA = buffer[index + 3];

                            if (currentA == 0 || currentB == 255)
                            {
                                float distance = MathF.Sqrt(dx * dx + dy * dy);
                                float cosAngle = dx / distance;
                                float sinAngle = dy / distance;


                                byte bValue = (Math.Abs(distance - r) < 1) ? (byte)255 : (byte)0;

                                if (currentB == 255)
                                    bValue = 0;

                                if (bValue == 255)
                                {
                                    byte rValue = (byte)(cosAngle * 127 + 128);
                                    byte gValue = (byte)(sinAngle * 127 + 128);

                                    buffer[index] = 255;
                                    buffer[index + 1] = gValue;
                                    buffer[index + 2] = rValue;
                                    buffer[index + 3] = 255;

                                    Texturer._nextPoints.Add((x, y));
                                }
                                else
                                {
                                    byte rValue = clr.R;
                                    byte gValue = clr.G;
                                    bValue = clr.B;

                                    if (bValue == 0)
                                        bValue = 1;
                                    else if (bValue == 255)
                                        bValue = 254;

                                    buffer[index] = bValue;
                                    buffer[index + 1] = gValue;
                                    buffer[index + 2] = rValue;
                                    buffer[index + 3] = 255;
                                }
                            }
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

        public static unsafe Vector2 GetDaemonAngleFast(WriteableBitmap wbmp, int x, int y)
        {
            wbmp.Lock();
            byte* pixelPtr = (byte*)wbmp.BackBuffer + y * wbmp.BackBufferStride + x * 4;

            byte bValue = pixelPtr[0];

            if (bValue != 255)
            {
                wbmp.Unlock();
                return Vector2.Zero;
            }

            byte rValue = pixelPtr[2];
            byte gValue = pixelPtr[1];

            float sin = (float)(gValue - 128f) / 127;
            float cos = (float)(rValue - 128f) / 127;
            wbmp.Unlock();

            return new Vector2(cos, sin);
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

        public static WriteableBitmap Copy(WriteableBitmap source)
        {
            WriteableBitmap copy = new WriteableBitmap(
                source.PixelWidth,
                source.PixelHeight,
                source.DpiX,
                source.DpiY,
                source.Format,
                source.Palette
            );

            int stride = source.PixelWidth * ((source.Format.BitsPerPixel + 7) / 8);
            byte[] pixels = new byte[stride * source.PixelHeight];
            source.CopyPixels(pixels, stride, 0);
            copy.WritePixels(new Int32Rect(0, 0, source.PixelWidth, source.PixelHeight),
                            pixels, stride, 0);

            return copy;
        }

        public static void FillTextureCircleWithAlpha(
     WriteableBitmap bitmap,
     Vector2 center,
     WriteableBitmap tex,
     ushort island)
        {
            int x0 = (int)center.X;
            int y0 = (int)center.Y;
            int r = tex.PixelWidth / 2;

            bitmap.Lock();
            tex.Lock();

            try
            {
                unsafe
                {
                    byte* bufferBitmap = (byte*)bitmap.BackBuffer;
                    int strideBitmap = bitmap.BackBufferStride;

                    byte* bufferTex = (byte*)tex.BackBuffer;
                    int strideTex = tex.BackBufferStride;

                    int left = Math.Max(0, x0 - r);
                    int right = Math.Min(bitmap.PixelWidth - 1, x0 + r);
                    int top = Math.Max(0, y0 - r);
                    int bottom = Math.Min(bitmap.PixelHeight - 1, y0 + r);

                    int rSquared = r * r;

                    for (int y = top; y <= bottom; y++)
                    {
                        int dy = y - y0;
                        int dySquared = dy * dy;
                        if (dySquared > rSquared) continue;

                        int xLimit = (int)Math.Sqrt(rSquared - dySquared);
                        int xStart = Math.Max(left, x0 - xLimit);
                        int xEnd = Math.Min(right, x0 + xLimit);

                        for (int x = xStart; x <= xEnd; x++)
                        {
                            int texX = r + (x - x0);
                            int texY = r + (y - y0);

                            if (Texturer.GetIsland(x, y) != island) continue;

                            int bitmapIdx = y * strideBitmap + x * 4;
                            int texIdx = texY * strideTex + texX * 4;

                            byte srcB = bufferTex[texIdx];
                            byte srcG = bufferTex[texIdx + 1];
                            byte srcR = bufferTex[texIdx + 2];
                            byte srcA = bufferTex[texIdx + 3];

                            if (srcA == 0) continue;

                            byte dstB = bufferBitmap[bitmapIdx];
                            byte dstG = bufferBitmap[bitmapIdx + 1];
                            byte dstR = bufferBitmap[bitmapIdx + 2];
                            byte dstA = bufferBitmap[bitmapIdx + 3];

                            if (srcA == 255)
                            {
                                bufferBitmap[bitmapIdx] = srcB;
                                bufferBitmap[bitmapIdx + 1] = srcG;
                                bufferBitmap[bitmapIdx + 2] = srcR;
                                bufferBitmap[bitmapIdx + 3] = 255;
                                continue;
                            }

                            if (dstA == 0)
                            {
                                bufferBitmap[bitmapIdx] = srcB;
                                bufferBitmap[bitmapIdx + 1] = srcG;
                                bufferBitmap[bitmapIdx + 2] = srcR;
                                bufferBitmap[bitmapIdx + 3] = srcA;
                                continue;
                            }

                            float sA = srcA / 255f;
                            float dA = dstA / 255f;
                            float oA = sA + dA * (1 - sA);

                            if (oA < 0.001f)
                            {
                                bufferBitmap[bitmapIdx] = 0;
                                bufferBitmap[bitmapIdx + 1] = 0;
                                bufferBitmap[bitmapIdx + 2] = 0;
                                bufferBitmap[bitmapIdx + 3] = 0;
                                continue;
                            }

                            float oB = srcB * sA + dstB * dA * (1 - sA);
                            float oG = srcG * sA + dstG * dA * (1 - sA);
                            float oR = srcR * sA + dstR * dA * (1 - sA);

                            bufferBitmap[bitmapIdx] = (byte)Math.Clamp(oB, 0, 255);
                            bufferBitmap[bitmapIdx + 1] = (byte)Math.Clamp(oG, 0, 255);
                            bufferBitmap[bitmapIdx + 2] = (byte)Math.Clamp(oR, 0, 255);
                            bufferBitmap[bitmapIdx + 3] = (byte)(oA * 255);
                        }
                    }
                }
            }
            finally
            {
                int dirtyLeft = Math.Max(0, x0 - r);
                int dirtyTop = Math.Max(0, y0 - r);
                int dirtyWidth = Math.Min(bitmap.PixelWidth, x0 + r) - dirtyLeft;
                int dirtyHeight = Math.Min(bitmap.PixelHeight, y0 + r) - dirtyTop;

                if (dirtyWidth > 0 && dirtyHeight > 0)
                {
                    bitmap.AddDirtyRect(new Int32Rect(dirtyLeft, dirtyTop, dirtyWidth, dirtyHeight));
                }

                tex.Unlock();
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

        public static Color GetPixel(WriteableBitmap wbmp, int x, int y)
        {
            byte[] pixels = new byte[4];
            wbmp.CopyPixels(new Int32Rect(x, y, 1, 1), pixels, 4, 0);
            return Color.FromArgb(pixels[3], pixels[2], pixels[1], pixels[0]);
        }

        public static unsafe byte GetPixelAlphaFast(WriteableBitmap wbmp, int x, int y)
        {
            wbmp.Lock();
            byte alpha = *((byte*)wbmp.BackBuffer + y * wbmp.BackBufferStride + x * 4 + 3);
            wbmp.Unlock();
            return alpha;
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

        public static WriteableBitmap ScaleBitmap(WriteableBitmap source, int newWidth, int newHeight)
        {
            BitmapImage bitmapImage = new BitmapImage();

            using (MemoryStream stream = new MemoryStream())
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(source));
                encoder.Save(stream);

                stream.Seek(0, SeekOrigin.Begin);

                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
            }

            ScaleTransform scaleTransform = new ScaleTransform(
                (double)newWidth / bitmapImage.PixelWidth,
                (double)newHeight / bitmapImage.PixelHeight);

            TransformedBitmap transformedBitmap = new TransformedBitmap(bitmapImage, scaleTransform);

            return new WriteableBitmap(transformedBitmap);
        }

        public static WriteableBitmap RotateBitmap(WriteableBitmap source, float angle, bool cropToOriginalSize = true)
        {
            if (Math.Abs(angle % 90) < 0.01)
            {
                return RotateBitmap90Degrees(source, (int)angle);
            }

            var renderTarget = new RenderTargetBitmap(
                source.PixelWidth, source.PixelHeight,
                96, 96, PixelFormats.Pbgra32);

            var visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                TransformGroup transformGroup = new TransformGroup();

                if (cropToOriginalSize)
                {
                    RotateTransform rotateTransform = new RotateTransform(angle);
                    rotateTransform.CenterX = source.PixelWidth / 2.0;
                    rotateTransform.CenterY = source.PixelHeight / 2.0;
                    transformGroup.Children.Add(rotateTransform);
                }
                else
                {
                    double angleRad = angle * Math.PI / 180.0;
                    double sin = Math.Abs(Math.Sin(angleRad));
                    double cos = Math.Abs(Math.Cos(angleRad));

                    int newWidth = (int)(source.PixelWidth * cos + source.PixelHeight * sin);
                    int newHeight = (int)(source.PixelWidth * sin + source.PixelHeight * cos);

                    double offsetX = (newWidth - source.PixelWidth) / 2.0;
                    double offsetY = (newHeight - source.PixelHeight) / 2.0;

                    transformGroup.Children.Add(new TranslateTransform(offsetX, offsetY));

                    RotateTransform rotateTransform = new RotateTransform(angle);
                    rotateTransform.CenterX = source.PixelWidth / 2.0;
                    rotateTransform.CenterY = source.PixelHeight / 2.0;
                    transformGroup.Children.Add(rotateTransform);
                }

                context.PushTransform(transformGroup);
                context.DrawImage(source, new Rect(0, 0, source.PixelWidth, source.PixelHeight));
            }

            renderTarget.Render(visual);
            return new WriteableBitmap(renderTarget);
        }

        public static WriteableBitmap CropToSquare(WriteableBitmap source)
        {
            int size = Math.Min(source.PixelWidth, source.PixelHeight);
            int x = (source.PixelWidth - size) / 2;
            int y = (source.PixelHeight - size) / 2;

            return CropBitmap(source, x, y, size, size);
        }

        private static WriteableBitmap CropBitmap(WriteableBitmap source, int x, int y, int width, int height)
        {
            WriteableBitmap cropped = new WriteableBitmap(
                width, height,
                source.DpiX, source.DpiY,
                source.Format, source.Palette);

            cropped.Lock();
            source.Lock();

            try
            {
                int sourceStride = source.BackBufferStride;
                int destStride = cropped.BackBufferStride;
                int bytesPerPixel = (source.Format.BitsPerPixel + 7) / 8;

                unsafe
                {
                    byte* sourcePtr = (byte*)source.BackBuffer;
                    byte* destPtr = (byte*)cropped.BackBuffer;

                    for (int row = 0; row < height; row++)
                    {
                        int sourceIndex = ((y + row) * sourceStride) + (x * bytesPerPixel);
                        int destIndex = row * destStride;

                        Buffer.MemoryCopy(
                            sourcePtr + sourceIndex,
                            destPtr + destIndex,
                            width * bytesPerPixel,
                            width * bytesPerPixel);
                    }
                }
            }
            finally
            {
                source.Unlock();
                cropped.Unlock();
            }

            return cropped;
        }

        private static WriteableBitmap RotateBitmap90Degrees(WriteableBitmap source, int angle)
        {
            int newWidth = source.PixelWidth;
            int newHeight = source.PixelHeight;

            if (angle % 180 != 0)
            {
                newWidth = source.PixelHeight;
                newHeight = source.PixelWidth;
            }

            var renderTarget = new RenderTargetBitmap(
                newWidth, newHeight,
                96, 96, PixelFormats.Pbgra32);

            var visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                TransformGroup transformGroup = new TransformGroup();
                RotateTransform rotateTransform = new RotateTransform(angle);

                if (angle % 180 != 0)
                {
                    rotateTransform.CenterX = source.PixelWidth / 2.0;
                    rotateTransform.CenterY = source.PixelHeight / 2.0;

                    double offsetX = (newWidth - source.PixelWidth) / 2.0;
                    double offsetY = (newHeight - source.PixelHeight) / 2.0;
                    transformGroup.Children.Add(new TranslateTransform(offsetX, offsetY));
                }

                transformGroup.Children.Add(rotateTransform);

                context.PushTransform(transformGroup);
                context.DrawImage(source, new Rect(0, 0, source.PixelWidth, source.PixelHeight));
            }

            renderTarget.Render(visual);
            return new WriteableBitmap(renderTarget);
        }

        private static float SmoothStep(float edge0, float edge1, float x)
        {
            x = Math.Clamp((x - edge0) / (edge1 - edge0), 0f, 1f);

            return x * x * (3f - 2f * x);
        }

        public static byte[] CreateSoftCircleMaskOptimized(int size)
        {
            byte[] mask = new byte[size * size];
            float center = size / 2f;
            float outerRadius = center;
            float innerRadius = outerRadius / 2f;

            float[] radialMask = new float[size / 2 + 1];

            for (int r = 0; r <= size / 2; r++)
            {
                float distance = r;

                if (distance <= innerRadius)
                {
                    radialMask[r] = 255f;
                }
                else if (distance >= outerRadius)
                {
                    radialMask[r] = 0f;
                }
                else
                {
                    float t = (distance - innerRadius) / (outerRadius - innerRadius);
                    radialMask[r] = 255f * (1f - SmoothStep(0f, 1f, t));
                }
            }

            // Symmetry speedUp
            for (int y = 0; y < size; y++)
            {
                float dy = y - center + 0.5f;
                float dySq = dy * dy;

                for (int x = 0; x < size; x++)
                {
                    float dx = x - center + 0.5f;
                    float distance = (float)Math.Sqrt(dx * dx + dySq);

                    // Closest
                    int index = (int)Math.Round(distance);
                    if (index > size / 2) index = size / 2;

                    mask[y * size + x] = (byte)radialMask[index];
                }
            }

            return mask;
        }

        public static byte[] CreateLogarithmicMask(int size)
        {
            byte[] mask = new byte[size * size];
            float center = size / 2f;
            float outerRadius = center;
            float innerRadius = outerRadius / 2f;

            float logBase = 40;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center + 0.5f;
                    float dy = y - center + 0.5f;
                    float distance = (float)Math.Sqrt(dx * dx + dy * dy);

                    if (distance <= innerRadius)
                    {
                        mask[y * size + x] = 255;
                    }
                    else if (distance >= outerRadius)
                    {
                        mask[y * size + x] = 0;
                    }
                    else
                    {
                        float t = (distance - innerRadius) / (outerRadius - innerRadius);

                        float logValue = (float)Math.Log(1 + t * (logBase - 1), logBase);

                        logValue = SmoothStep(0f, 1f, logValue);

                        float alpha = 255f * (1f - logValue);
                        mask[y * size + x] = (byte)Math.Clamp(alpha, 0, 255);
                    }
                }
            }

            return mask;
        }

        public static WriteableBitmap ApplyMask(WriteableBitmap source, byte[] mask)
        {
            int size = (int)Math.Sqrt(mask.Length);
            var result = new WriteableBitmap(size, size, 96, 96, PixelFormats.Pbgra32, null);

            int stride = source.PixelWidth * 4;
            byte[] sourcePixels = new byte[stride * source.PixelHeight];
            source.CopyPixels(sourcePixels, stride, 0);

            byte[] resultPixels = new byte[size * size * 4];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int maskIndex = y * size + x;
                    float alpha = mask[maskIndex] / 255f;

                    int srcX = (int)((x / (float)size) * source.PixelWidth);
                    int srcY = (int)((y / (float)size) * source.PixelHeight);

                    srcX = Math.Clamp(srcX, 0, source.PixelWidth - 1);
                    srcY = Math.Clamp(srcY, 0, source.PixelHeight - 1);

                    int srcIndex = (srcY * source.PixelWidth + srcX) * 4;
                    int dstIndex = maskIndex * 4;

                    float srcAlpha = sourcePixels[srcIndex + 3] / 255f;
                    float finalAlpha = srcAlpha * alpha;

                    for (int i = 0; i < 3; i++)
                    {
                        float color = sourcePixels[srcIndex + i] / 255f;
                        resultPixels[dstIndex + i] = (byte)(color * finalAlpha * 255);
                    }
                    resultPixels[dstIndex + 3] = (byte)(finalAlpha * 255);
                }
            }

            result.WritePixels(new Int32Rect(0, 0, size, size),
                              resultPixels, size * 4, 0);

            return result;
        }
    }
}