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
using System.Xml.Linq;

namespace Turner1
{
    public class PaintingEncoding
    {
        public int PaintingIndex
        {
            get;
            set;
        }

        public bool Rotated
        {
            get;
            set;
        }

        public bool FrontVisible
        {
            get;
            set;
        }

        public PaintingEncoding(XElement configuration)
        {
            XElement paintingIndexElement = configuration.Element("PaintingIndex");
            PaintingIndex = int.Parse(paintingIndexElement.Value);

            XElement rotatedElement = configuration.Element("Rotated");
            Rotated = bool.Parse(rotatedElement.Value);

            XElement frontVisibleElement = configuration.Element("FrontVisible");
            FrontVisible = bool.Parse(frontVisibleElement.Value);
        }

        public PaintingEncoding(int paintingIndex, bool rotated, bool frontVisible)
        {
            PaintingIndex = paintingIndex;
            Rotated = rotated;
            FrontVisible = frontVisible;
        }

        public PaintingEncoding(PaintingEncoding toCopy)
        {
            PaintingIndex = toCopy.PaintingIndex;
            Rotated = toCopy.Rotated; 
            FrontVisible = toCopy.FrontVisible; 
        }

        public void Mutate(double mutationRate)
        {
            if (MainPage.rand.NextDouble() < mutationRate)
            {
                if (MainPage.FlipCoin())
                {
                    if (Rotated)
                    {
                        Rotated = false;
                    }
                    else
                    {
                        Rotated = true;
                    }
                }
            }

            if (MainPage.rand.NextDouble() < mutationRate)
            {
                if (MainPage.FlipCoin())
                {
                    if (FrontVisible)
                    {
                        FrontVisible = false;
                    }
                    else
                    {
                        FrontVisible = true;
                    }
                }
            }
         }
            public XElement ToXml()
        {
            XElement paintingEncodingElement = new XElement("PaintingEncoding");
            XElement paintingIndexElement =  new XElement("PaintingIndex");
            XText paintingIndexText = new XText(PaintingIndex.ToString());
            paintingIndexElement.Add(paintingIndexText);
            paintingEncodingElement.Add(paintingIndexElement);

            XElement rotatedElement = new XElement("Rotated");
            XText rotatedText = new XText(Rotated.ToString());
            rotatedElement.Add(rotatedText);
            paintingEncodingElement.Add(rotatedElement);

            XElement frontVisibleElement = new XElement("FrontVisible");
            XText frontVisibleText = new XText(FrontVisible.ToString());
            frontVisibleElement.Add(frontVisibleText);
            paintingEncodingElement.Add(frontVisibleElement);


            return paintingEncodingElement;
        }
      
    }
}
