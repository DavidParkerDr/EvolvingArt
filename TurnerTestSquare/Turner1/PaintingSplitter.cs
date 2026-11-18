using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Resources;
using System.Xml.Linq;
using System.Collections.Generic;

namespace Turner1
{
    public class PaintingSplitter
    {
       
        public static void SplitPainting(int paintingChoice, int canvasId, int dimensionsMultiplier, PaintingGrid parent)
        {
            GenerateBitmap(paintingChoice, canvasId, dimensionsMultiplier, parent);

        }

        public static void SplitPainting(int paintingChoice, int canvasId, int dimensionsMultiplier, GeneticAlgorithm parent)
        {
            GenerateBitmap(paintingChoice, canvasId, dimensionsMultiplier, parent);

        }

        private static void GenerateBitmap(int paintingChoice, int canvasId, int dimensionsMultiplier, PaintingGrid parent)
        {

            PasteBitmap(GetWriteableBitmap(paintingChoice, canvasId, "a"), GetWriteableBitmap(paintingChoice, canvasId, "b"), dimensionsMultiplier, parent, paintingChoice, canvasId);
                    
            
        }
        private static void GenerateBitmap(int paintingChoice, int canvasId, int dimensionsMultiplier, GeneticAlgorithm parent)
        {

            PasteBitmap(GetWriteableBitmap(paintingChoice, canvasId, "a"), GetWriteableBitmap(paintingChoice, canvasId, "b"), dimensionsMultiplier, parent);


        }

        private static void PasteBitmap(WriteableBitmap frontBitmap, WriteableBitmap backBitmap, int dimensionsMultiplier, GeneticAlgorithm parent)
        {

            int endX = frontBitmap.PixelWidth / dimensionsMultiplier;
            int endY = frontBitmap.PixelHeight / dimensionsMultiplier;

            for (int cellY = 0; cellY < dimensionsMultiplier; cellY++)
            {
                for (int cellX = 0; cellX < dimensionsMultiplier; cellX++)
                {
                    WriteableBitmap newFrontBitmap = new WriteableBitmap(endX, endY);
                    WriteableBitmap newBackBitmap = new WriteableBitmap(endX, endY);
                    int startX = 0;
                    int startY = 0;
                    for (int row = startY; row < endY; row++)
                    {
                        for (int col = startX; col < endX; col++)
                        {
                            int rowOffset = cellY * endY;
                            int colOffset = cellX * endX;
                            int basePixelIndex = (row * endX) + col;
                            int frontPixelIndex = ((row + rowOffset) * frontBitmap.PixelWidth) + col + colOffset;
                            int frontPixelAsInt = frontBitmap.Pixels[frontPixelIndex];
                            newFrontBitmap.Pixels[basePixelIndex] = frontPixelAsInt;

                            int backCellX = dimensionsMultiplier - cellX - 1;
                            int backColOffset = backCellX * endX;
                            int backPixelIndex = ((row + rowOffset) * backBitmap.PixelWidth) + col + backColOffset;
                            int backPixelAsInt = backBitmap.Pixels[backPixelIndex];
                            newBackBitmap.Pixels[basePixelIndex] = backPixelAsInt;
                        }
                    }
                    // make a new painting with the front and back supplied
                    ImageHolder painting = new ImageHolder(newFrontBitmap, newBackBitmap);
                    parent.AddImage(painting);
                }
            }

        }

        private static void PasteBitmap(WriteableBitmap frontBitmap, WriteableBitmap backBitmap, int dimensionsMultiplier, PaintingGrid parent, int paintingChoice, int canvasId)
        {
            
            int endX = frontBitmap.PixelWidth / dimensionsMultiplier;
            int endY = frontBitmap.PixelHeight / dimensionsMultiplier;

            for (int cellY = 0; cellY < dimensionsMultiplier; cellY++) 
            {
                for (int cellX = 0; cellX < dimensionsMultiplier; cellX++)
                {
                    WriteableBitmap newFrontBitmap = new WriteableBitmap(endX, endY);
                    WriteableBitmap newBackBitmap = new WriteableBitmap(endX, endY);
                    int startX = 0;
                    int startY = 0;
                    for (int row = startY; row < endY; row++)
                    {
                        for (int col = startX; col < endX; col++)
                        {
                            int rowOffset = cellY * endY;
                            int colOffset = cellX * endX;
                            int basePixelIndex = (row * endX) + col;
                            int frontPixelIndex = ((row + rowOffset) * frontBitmap.PixelWidth) + col + colOffset;
                            int frontPixelAsInt = frontBitmap.Pixels[frontPixelIndex];
                            newFrontBitmap.Pixels[basePixelIndex] = frontPixelAsInt;

                            int backCellX = dimensionsMultiplier - cellX-1;
                            int backColOffset = backCellX * endX;
                            int backPixelIndex = ((row + rowOffset) * backBitmap.PixelWidth) + col + backColOffset;
                            int backPixelAsInt = backBitmap.Pixels[backPixelIndex];
                            newBackBitmap.Pixels[basePixelIndex] = backPixelAsInt;
                        }
                    }
                    // make a new painting with the front and back supplied
                    Painting painting = new Painting(newFrontBitmap, newBackBitmap, parent.NumPaintings(), parent);
                    if (paintingChoice == 1)
                    {
                        Uri frontSoundSource = new Uri(string.Format("Sounds/Squares/Roberto{0}/{1}a.mp3", paintingChoice, canvasId), UriKind.Relative);
                        Uri backSoundSource = new Uri(string.Format("Sounds/Squares/Roberto{0}/{1}b.mp3", paintingChoice, canvasId), UriKind.Relative);
                        painting.AddFrontSound(frontSoundSource);
                        painting.AddBackSound(backSoundSource);
                    }
                    parent.AddPainting(painting);
                }
            }
            
        }



        private static WriteableBitmap GetWriteableBitmap(int paintingChoice, int canvasId, string side)
        {
            Uri uri = new Uri(string.Format("Images/Squares/Roberto{0}/{1}{2}.png", paintingChoice, canvasId, side), UriKind.Relative);
            StreamResourceInfo sri = Application.GetResourceStream(uri);

            BitmapImage src = new BitmapImage();
            src.SetSource(sri.Stream);
            return GetWriteableBitmap(src);
           // WriteableBitmap image = new WriteableBitmap(src);

           // BitmapImage bitmapImage = new BitmapImage();
           // bitmapImage.CreateOptions = BitmapCreateOptions.None;
//bitmapImage.UriSource = uri;
            //return GetWriteableBitmap(bitmapImage);
            

            
        }

        private static WriteableBitmap GetWriteableBitmap(BitmapImage bitmapImage)
        {
            return new WriteableBitmap(bitmapImage);
        }


        private static int GetPixelColour(Color color)
        {
            // generate a color in Pbgra32 format
            byte[] components = new byte[4];
            components[0] = color.B;        // blue
            components[1] = color.G;        // green
            components[2] = color.R;        // red
            components[3] = color.A;      // unused

            // you could certainly do your own masking here
            int pixelColour = BitConverter.ToInt32(components, 0);

            return pixelColour;
        }

        private static int GetPixelColour(int a, int r, int g, int b)
        {
            return GetPixelColour(Color.FromArgb((byte)(a % 255), (byte)(r % 255), (byte)(g % 255), (byte)(b % 255)));
        }


       


    }
}
