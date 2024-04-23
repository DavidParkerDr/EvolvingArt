using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Media.Imaging;
using System.Collections.Generic;

namespace Turner1
{
    public class ImageHolder
    {
        WriteableBitmap _imageFront;
        public WriteableBitmap PaintingImageFront
        {
            get
            {
                return _imageFront;
            }
            set
            {
                _imageFront = value;
            }
        }
        WriteableBitmap _imageBack;
        public WriteableBitmap PaintingImageBack
        {
            get
            {
                return _imageBack;
            }
            set
            {
                _imageBack = value;
            }
        }

        public ImageHolder(Uri frontUri, Uri backUri)
        {
            BitmapImage bitmapImageFront = new BitmapImage();
            bitmapImageFront.CreateOptions = BitmapCreateOptions.None;
            bitmapImageFront.UriSource = frontUri;
            _imageFront = new WriteableBitmap(bitmapImageFront);

            BitmapImage bitmapImageBack = new BitmapImage();
            bitmapImageBack.CreateOptions = BitmapCreateOptions.None;
            bitmapImageBack.UriSource = frontUri;
            _imageBack = new WriteableBitmap(bitmapImageBack);
        }

        public List<Pixel> GetAllPixels(WriteableBitmap wb)
        {
            List<Pixel> pixels = new List<Pixel>();
            
            for (int i = 0; i < wb.Pixels.Length; i++)
            {
                int pixelAsInt = wb.Pixels[i];
                Pixel pixel = new Pixel(pixelAsInt);
                pixels.Add(pixel);
            }
            return pixels;
        }

        public List<Pixel> GetAllPixels(bool rotated, bool frontVisible)
        {
            List<Pixel> pixels = null;
            WriteableBitmap image = null;
            if (frontVisible)
            {
                image = PaintingImageFront;
            }
            else
            {
                image = PaintingImageBack;
            }

            pixels = GetAllPixels(image);
            

            return pixels;

        }

        public List<Pixel> GetLeftEdgePixels(WriteableBitmap wb, int edgeThickness, bool invert = false)
        {
            List<Pixel> pixels = new List<Pixel>();
            int pixelHeight = wb.PixelHeight;
            int pixelWidth = wb.PixelWidth;
            for (int i = 0; i < pixelHeight; i++)
            {
                int a = 0;
                int r = 0;
                int g = 0;
                int b = 0;
                
                for (int j = 0; j < edgeThickness; j++)
                {
                    int pixelAsInt = wb.Pixels[(i * pixelWidth) + j];
                    Pixel pixel = new Pixel(pixelAsInt);
                    a += pixel.A;
                    r += pixel.R;
                    g += pixel.G;
                    b += pixel.B;
                    
                }
                Pixel averagePixel = new Pixel(a / edgeThickness, r / edgeThickness, g / edgeThickness, b / edgeThickness);
                if (invert)
                {
                    pixels.Insert(0, averagePixel);
                }
                else
                {
                    pixels.Add(averagePixel);
                }
            }
            return pixels;

        }

        public List<Pixel> GetRightEdgePixels(WriteableBitmap wb, int edgeThickness, bool invert = false)
        {
            List<Pixel> pixels = new List<Pixel>();
            int pixelHeight = wb.PixelHeight;
            int pixelWidth = wb.PixelWidth;
            for (int i = 0; i < pixelHeight; i++)
            {
                int a = 0;
                int r = 0;
                int g = 0;
                int b = 0;
                for (int j = 0; j < edgeThickness; j++)
                {
                    int pixelAsInt = wb.Pixels[((pixelWidth - 1) + (i * pixelWidth)) - j];
                    Pixel pixel = new Pixel(pixelAsInt);
                    a += pixel.A;
                    r += pixel.R;
                    g += pixel.G;
                    b += pixel.B;
                    
                }
                Pixel averagePixel = new Pixel(a / edgeThickness, r / edgeThickness, g / edgeThickness, b / edgeThickness);
                if (invert)
                {
                    pixels.Insert(0, averagePixel);
                }
                else
                {
                    pixels.Add(averagePixel);
                }
            }
            return pixels;

        }

        public List<Pixel> GetTopEdgePixels(WriteableBitmap wb, int edgeThickness, bool invert = false)
        {
            List<Pixel> pixels = new List<Pixel>();
            int pixelHeight = wb.PixelHeight;
            int pixelWidth = wb.PixelWidth;
            for (int i = 0; i < pixelWidth; i++)
            {
                int a = 0;
                int r = 0;
                int g = 0;
                int b = 0;

                for (int j = 0; j < edgeThickness; j++)
                {
                    int pixelAsInt = wb.Pixels[i + (j * pixelWidth) ];
                    Pixel pixel = new Pixel(pixelAsInt);
                    a += pixel.A;
                    r += pixel.R;
                    g += pixel.G;
                    b += pixel.B;
                }
                Pixel averagePixel = new Pixel(a / edgeThickness, r / edgeThickness, g / edgeThickness, b / edgeThickness);
                if (invert)
                {
                    pixels.Insert(0, averagePixel);
                }
                else
                {
                    pixels.Add(averagePixel);
                }
            }
            return pixels;

        }

        public List<Pixel> GetBottomEdgePixels(WriteableBitmap wb, int edgeThickness, bool invert = false)
        {
            List<Pixel> pixels = new List<Pixel>();
            int pixelHeight = wb.PixelHeight;
            int pixelWidth = wb.PixelWidth;
            for (int i = 0; i < pixelWidth; i++)
            {
                int a = 0;
                int r = 0;
                int g = 0;
                int b = 0;

                for (int j = 0; j < edgeThickness; j++)
                {
                    int pixelAsInt = wb.Pixels[((pixelHeight - 1) * pixelWidth) + i - (j * pixelWidth)];
                    Pixel pixel = new Pixel(pixelAsInt);
                    a += pixel.A;
                    r += pixel.R;
                    g += pixel.G;
                    b += pixel.B;
                }
                Pixel averagePixel = new Pixel(a / edgeThickness, r / edgeThickness, g / edgeThickness, b / edgeThickness);
                if (invert)
                {
                    pixels.Insert(0, averagePixel);
                }
                else
                {
                    pixels.Add(averagePixel);
                }
            }
            return pixels;

        }

        public List<Pixel> GetLeftEdgePixels(bool rotated, bool frontVisible, int edgeThickness)
        {
            List<Pixel> pixels = null;
            WriteableBitmap image = null;
            if (frontVisible)
            {
                image = PaintingImageFront;
            }
            else
            {
                image = PaintingImageBack;
            }

            if (rotated)
            {
                pixels = GetRightEdgePixels(image, edgeThickness, true);
            }
            else
            {
                pixels = GetLeftEdgePixels(image, edgeThickness);
            }

            return pixels;

        }

        public List<Pixel> GetRightEdgePixels(bool rotated, bool frontVisible, int edgeThickness)
        {
            List<Pixel> pixels = null;
            WriteableBitmap image = null;
            if (frontVisible)
            {
                image = PaintingImageFront;
            }
            else
            {
                image = PaintingImageBack;
            }

            if (rotated)
            {
                pixels = GetLeftEdgePixels(image, edgeThickness, true);
            }
            else
            {
                pixels = GetRightEdgePixels(image, edgeThickness);
            }

            return pixels;

        }

        public List<Pixel> GetTopEdgePixels(bool rotated, bool frontVisible, int edgeThickness)
        {
            List<Pixel> pixels = null;
            WriteableBitmap image = null;
            if (frontVisible)
            {
                image = PaintingImageFront;
            }
            else
            {
                image = PaintingImageBack;
            }

            if (rotated)
            {
                pixels = GetBottomEdgePixels(image, edgeThickness, true);
            }
            else
            {
                pixels = GetTopEdgePixels(image, edgeThickness);
            }

            return pixels;

        }

        public List<Pixel> GetBottomEdgePixels(bool rotated, bool frontVisible, int edgeThickness)
        {
            List<Pixel> pixels = null;
            WriteableBitmap image = null;
            if (frontVisible)
            {
                image = PaintingImageFront;
            }
            else
            {
                image = PaintingImageBack;
            }

            if (rotated)
            {
                pixels = GetTopEdgePixels(image, edgeThickness, true);
            }
            else
            {
                pixels = GetBottomEdgePixels(image, edgeThickness);
            }

            return pixels;

        }
    }
}
