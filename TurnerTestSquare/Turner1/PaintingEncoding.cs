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

        public int Rotation
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

            XElement rotationElement = configuration.Element("Rotation");
            Rotation = int.Parse(rotationElement.Value);

            XElement frontVisibleElement = configuration.Element("FrontVisible");
            FrontVisible = bool.Parse(frontVisibleElement.Value);
        }

        public PaintingEncoding(int paintingIndex, int rotation, bool frontVisible)
        {
            PaintingIndex = paintingIndex;
            Rotation = rotation;
            FrontVisible = frontVisible;
        }

        public PaintingEncoding(PaintingEncoding toCopy)
        {
            PaintingIndex = toCopy.PaintingIndex;
            Rotation = toCopy.Rotation; 
            FrontVisible = toCopy.FrontVisible; 
        }

        public void Mutate(double mutationRate)
        {
            if (MainPage.rand.NextDouble() < mutationRate)
            {
                double random = MainPage.rand.NextDouble();
                if (random <= 0.25)
                {
                    Rotation = 0;
                }
                else if (random <= 0.5)
                {
                    Rotation = 90;
                }
                else if (random <= 0.75)
                {
                    Rotation = 180;
                }
                else if (random <= 1)
                {
                    Rotation = 270;
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

            XElement rotationElement = new XElement("Rotation");
            XText rotationText = new XText(Rotation.ToString());
            rotationElement.Add(rotationText);
            paintingEncodingElement.Add(rotationElement);

            XElement frontVisibleElement = new XElement("FrontVisible");
            XText frontVisibleText = new XText(FrontVisible.ToString());
            frontVisibleElement.Add(frontVisibleText);
            paintingEncodingElement.Add(frontVisibleElement);


            return paintingEncodingElement;
        }
      
    }
}
