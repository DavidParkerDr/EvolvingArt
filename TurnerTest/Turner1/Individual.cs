using System;
using System.Net;
using System.Windows;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace Turner1
{
    public class Individual : IComparable<Individual>
    {
        public double Fitness
        {
            get;
            set;
        }

        public PaintingGridEncoding Encoding
        {
            get;
            set;
        }

        public GeneticAlgorithm Parent
        {
            get;
            set;
        }

        public Individual(GeneticAlgorithm parent, PaintingGridEncoding encoding = null)
        {
            Parent = parent;
            Encoding = encoding;
            if (Encoding == null)
            {
                Encoding = new PaintingGridEncoding();
            }
            CalculateFitness();
        }

        public Individual(GeneticAlgorithm parent, XElement individualElement)
        {
            Parent = parent;
            // process xml 
            Encoding = new PaintingGridEncoding(individualElement.Element("PaintingGridEncoding"));
            if (Encoding == null)
            {
                Encoding = new PaintingGridEncoding();
            }

            Fitness = double.Parse(individualElement.Element("Fitness").Value);
        }

        public void CalculateFitness()
        {
            if (Parent.FitnessType == 0)
            {
                CalculateFitness0();
            }
            else
            {
                CalculateFitness1();
            }
        }

        public void CalculateFitness0()
        {

            Fitness = 0.0;
            int numberOfPixels = 0;
            double distanceSum = 0;

            for (int row = 0; row < MainPage.NUMBER_OF_ROWS; row++)
            {
                for (int column = 0; column < MainPage.NUMBER_OF_COLUMNS; column++)
                {
                    int index = (row * MainPage.NUMBER_OF_COLUMNS) + column;
                    PaintingEncoding paintingEncoding = Encoding.PaintingEncodingAt(index);
                    List<Pixel> pixels = Parent.GetAllPixels(paintingEncoding);
                    //average the pixels
                    int a = 0;
                    int r = 0;
                    int g = 0;
                    int b = 0;

                    for (int pixelIndex = 0; pixelIndex < pixels.Count; pixelIndex++)
                    {
                        Pixel pixel = pixels[pixelIndex];
                        a += pixel.A;
                        r += pixel.R;
                        g += pixel.G;
                        b += pixel.B;
                    }

                    Pixel averagePixel = new Pixel(a / pixels.Count, r / pixels.Count, g / pixels.Count, b / pixels.Count);

                    // check one to the right
                    if (column < MainPage.NUMBER_OF_COLUMNS - 1)
                    {
                        PaintingEncoding paintingEncodingToRight = Encoding.PaintingEncodingAt(index + 1);

                        List<Pixel> rightPixels = Parent.GetAllPixels(paintingEncodingToRight);

                        //average the pixels
                        a = 0;
                        r = 0;
                        g = 0;
                        b = 0;

                        for (int pixelIndex = 0; pixelIndex < rightPixels.Count; pixelIndex++)
                        {
                            Pixel rightPixel = rightPixels[pixelIndex];
                            a += rightPixel.A;
                            r += rightPixel.R;
                            g += rightPixel.G;
                            b += rightPixel.B;
                        }

                        Pixel averageRightPixel = new Pixel(a / rightPixels.Count, r / rightPixels.Count, g / rightPixels.Count, b / rightPixels.Count);
                        double distance = averagePixel.Distance(averageRightPixel);
                        distanceSum += distance;
                        numberOfPixels++;
                    }

                    if (row < MainPage.NUMBER_OF_ROWS - 1)
                    {
                        // check one below
                        PaintingEncoding paintingEncodingBelow = Encoding.PaintingEncodingAt(index + MainPage.NUMBER_OF_COLUMNS);
                        List<Pixel> bottomPixels = Parent.GetAllPixels(paintingEncodingBelow);
                        //average the pixels
                        a = 0;
                        r = 0;
                        g = 0;
                        b = 0;

                        for (int pixelIndex = 0; pixelIndex < bottomPixels.Count; pixelIndex++)
                        {
                            Pixel bottomPixel = bottomPixels[pixelIndex];
                            a += bottomPixel.A;
                            r += bottomPixel.R;
                            g += bottomPixel.G;
                            b += bottomPixel.B;
                        }

                        Pixel averageBottomPixel = new Pixel(a / bottomPixels.Count, r / bottomPixels.Count, g / bottomPixels.Count, b / bottomPixels.Count);
                        double distance = averagePixel.Distance(averageBottomPixel);
                        distanceSum += distance;
                        numberOfPixels++;
                    }


                }
            }

            Fitness = distanceSum / numberOfPixels;

        }

        public void CalculateFitness1()
        {
            Fitness = 0.0;
            int numberOfPixels = 0;
            double distanceSum = 0;

            for (int row = 0; row < MainPage.NUMBER_OF_ROWS; row++)
            {
                for (int column = 0; column < MainPage.NUMBER_OF_COLUMNS; column++)
                {
                    int index = (row * MainPage.NUMBER_OF_COLUMNS) + column;
                    PaintingEncoding paintingEncoding = Encoding.PaintingEncodingAt(index);
                    List<Pixel> rightPixels = Parent.GetRightEdgePixels(paintingEncoding);
                    List<Pixel> bottomPixels = Parent.GetBottomEdgePixels(paintingEncoding);
                    // check one to the right
                    if (column < MainPage.NUMBER_OF_COLUMNS - 1)
                    {
                        PaintingEncoding paintingEncodingToRight = Encoding.PaintingEncodingAt(index+1);

                        List<Pixel> leftPixels = Parent.GetLeftEdgePixels(paintingEncodingToRight);

                        for (int pixelIndex = 0; pixelIndex < leftPixels.Count; pixelIndex++)
                        {
                            Pixel leftPixel = leftPixels[pixelIndex];
                            Pixel rightPixel = rightPixels[pixelIndex];
                            double distance = leftPixel.Distance(rightPixel);
                            distanceSum += distance;
                            numberOfPixels++;
                        }
                    }

                    if (row < MainPage.NUMBER_OF_ROWS - 1)
                    {
                        // check one below
                        PaintingEncoding paintingEncodingBelow = Encoding.PaintingEncodingAt(index + MainPage.NUMBER_OF_COLUMNS);
                        List<Pixel> topPixels = Parent.GetTopEdgePixels(paintingEncodingBelow);
                        for (int pixelIndex = 0; pixelIndex < topPixels.Count; pixelIndex++)
                        {
                            Pixel topPixel = topPixels[pixelIndex];
                            Pixel bottomPixel = bottomPixels[pixelIndex];
                            double distance = topPixel.Distance(bottomPixel);
                            distanceSum += distance;
                            numberOfPixels++;
                        }
                    }

                    
                }
            }

            Fitness = distanceSum / numberOfPixels;

            
            
        }
        public XElement ToXml()
        {
            XElement individualElement = new XElement("Individual");
            XElement fitnessElement = new XElement("Fitness");
            XText fitnessText = new XText(Fitness.ToString());
            fitnessElement.Add(fitnessText);
            individualElement.Add(fitnessElement);
            individualElement.Add(Encoding.ToXml());
            return individualElement;
        }

        public int CompareTo(Individual other)
        {
            return Fitness.CompareTo(other.Fitness);
        }
    }
}
