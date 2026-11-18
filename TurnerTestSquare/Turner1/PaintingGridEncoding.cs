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
    public class PaintingGridEncoding
    {
        List<PaintingEncoding> _paintingEncodings;
      

        public PaintingGridEncoding(XElement configuration)
        {
            _paintingEncodings = new List<PaintingEncoding>();
            XElement paintingChoice = configuration.Element("PaintingSelection");
            App.PaintingChoice = int.Parse(paintingChoice.Value);
            XElement multiplierElement = configuration.Element("DimensionMultiplier");
            MainPage.DIMENSIONS_MULTIPLIER = int.Parse(multiplierElement.Value);
            foreach (XElement paintingEncoding in configuration.Elements("PaintingEncoding"))
            {
                _paintingEncodings.Add(new PaintingEncoding(paintingEncoding));
            }
        }

        public PaintingGridEncoding(PaintingGridEncoding toCopy)
        {
            _paintingEncodings = new List<PaintingEncoding>();
            for (int i = 0; i < toCopy.NumberOfPaintingEncodings(); i++)
            {
                _paintingEncodings.Add(new PaintingEncoding(toCopy.PaintingEncodingAt(i)));
            }
        }

        public PaintingGridEncoding(bool empty = false)
        {
            _paintingEncodings = new List<PaintingEncoding>();

            if (!empty)
            {
                List<int> unPlaced = new List<int>();
                for (int i = 0; i < MainPage.NUMBER_OF_ROWS * MainPage.DIMENSIONS_MULTIPLIER * MainPage.NUMBER_OF_COLUMNS * MainPage.DIMENSIONS_MULTIPLIER; i++)
                {
                    unPlaced.Add(i);
                }

                int row = -1;
                int col = 0;

                while (unPlaced.Count > 0)
                {
                    if (col % MainPage.NUMBER_OF_COLUMNS * MainPage.DIMENSIONS_MULTIPLIER == 0)
                    {
                        row++;
                        col = 0;
                    }

                    int index = MainPage.rand.Next(unPlaced.Count);
                    int paintingIndex = unPlaced[index];
                    unPlaced.RemoveAt(index);

                    double random = MainPage.rand.NextDouble();
                    int rotation = 0;
                    if (random <= 0.25)
                    {
                        rotation = 0;
                    }
                    else if (random <= 0.5)
                    {
                        rotation = 90;
                    }
                    else if (random <= 0.75)
                    {
                        rotation = 180;
                    }
                    else if (random <= 1)
                    {
                        rotation = 270;
                    }

                    bool frontVisible = true;
                    if (MainPage.FlipCoin())
                    {
                        frontVisible = false;
                    }

                    _paintingEncodings.Add(new PaintingEncoding(paintingIndex, rotation, frontVisible));

                    col++;
                }
            }
        }

        public PaintingEncoding PaintingEncodingAt(int index)
        {
            return _paintingEncodings[index];
        }

        public int NumberOfPaintingEncodings()
        {
            return _paintingEncodings.Count;
        }

        public List<PaintingGridEncoding> Crossover(PaintingGridEncoding other)
        {
            return OrderCrossoverModified(other);
        }

        public void AddRemainingOrderCrossoverEncodings(PaintingGridEncoding other, int crossoverIndex1, int crossoverIndex2)
        {
            int indexOfCrossoverStart = 0;
            int index = crossoverIndex2 + 1;
            bool addAtEnd = true;
            if (index >= other.NumberOfPaintingEncodings())
            {
                index = 0;
                addAtEnd = false;
            }

            while (NumberOfPaintingEncodings() < other.NumberOfPaintingEncodings())
            {
                PaintingEncoding paintingEncoding = other.PaintingEncodingAt(index++);
                bool alreadyPresent = false;
                for (int i = 0; i < NumberOfPaintingEncodings(); i++)
                {
                    PaintingEncoding encoding = PaintingEncodingAt(i);
                    if (encoding.PaintingIndex == paintingEncoding.PaintingIndex)
                    {
                        alreadyPresent = true;
                        break;
                    }
                }
                if (!alreadyPresent)
                {
                    if (addAtEnd)
                    {
                        AddPaintingEncoding(new PaintingEncoding(paintingEncoding));
                    }
                    else
                    {
                        InsertPaintingEncoding(indexOfCrossoverStart++, new PaintingEncoding(paintingEncoding));
                    }
                }
                if (index >= other.NumberOfPaintingEncodings())
                {
                    index = 0;
                    addAtEnd = false;
                }
            }
        }

        public void CrossoverRotatedAndFrontVisible(PaintingGridEncoding parent1, PaintingGridEncoding parent2, int crossoverIndex1, int crossoverIndex2)
        {
            for (int i = 0; i < _paintingEncodings.Count; i++)
            {
                PaintingEncoding paintingEncoding = _paintingEncodings[i];
                if (MainPage.FlipCoin())
                {
                    PaintingEncoding otherPaintingEncoding = parent1.FindPaintingEncoding(paintingEncoding.PaintingIndex);
                    // select other for rotated value
                    if (i > crossoverIndex2 || i < crossoverIndex1)
                    {
                        // choose parent 1
                        otherPaintingEncoding = parent2.FindPaintingEncoding(paintingEncoding.PaintingIndex);
                    }

                    paintingEncoding.Rotation = otherPaintingEncoding.Rotation;
                }

                if (MainPage.FlipCoin())
                {
                    PaintingEncoding otherPaintingEncoding = parent1.FindPaintingEncoding(paintingEncoding.PaintingIndex);
                    // select other for rotated value
                    if (i > crossoverIndex2 || i < crossoverIndex1)
                    {
                        // choose parent 1
                        otherPaintingEncoding = parent2.FindPaintingEncoding(paintingEncoding.PaintingIndex);
                    }

                    paintingEncoding.FrontVisible = otherPaintingEncoding.FrontVisible;
                }
            }
        }

        public void CrossoverRotatedAndFrontVisibleModified(PaintingGridEncoding parent1, PaintingGridEncoding parent2)
        {
            for (int i = 0; i < _paintingEncodings.Count; i++)
            {
                PaintingEncoding paintingEncoding = _paintingEncodings[i];
                PaintingEncoding otherPaintingEncoding = null;
                if (MainPage.FlipCoin())
                {
                    otherPaintingEncoding = parent1.FindPaintingEncoding(paintingEncoding.PaintingIndex);


                }
                else
                {
                    otherPaintingEncoding = parent2.FindPaintingEncoding(paintingEncoding.PaintingIndex);
                }

                paintingEncoding.Rotation = otherPaintingEncoding.Rotation;

                otherPaintingEncoding = null;
                if (MainPage.FlipCoin())
                {
                    otherPaintingEncoding = parent1.FindPaintingEncoding(paintingEncoding.PaintingIndex);


                }
                else
                {
                    otherPaintingEncoding = parent2.FindPaintingEncoding(paintingEncoding.PaintingIndex);
                }

                paintingEncoding.FrontVisible = otherPaintingEncoding.FrontVisible;
            }
        }

        public List<PaintingGridEncoding> OrderCrossover(PaintingGridEncoding other)
        {


            int crossoverIndex1 = MainPage.rand.Next(MainPage.NUMBER_OF_ROWS * MainPage.DIMENSIONS_MULTIPLIER * MainPage.NUMBER_OF_COLUMNS * MainPage.DIMENSIONS_MULTIPLIER);
            int crossoverIndex2 = MainPage.rand.Next(MainPage.NUMBER_OF_ROWS * MainPage.DIMENSIONS_MULTIPLIER * MainPage.NUMBER_OF_COLUMNS * MainPage.DIMENSIONS_MULTIPLIER);

            if (crossoverIndex1 > crossoverIndex2)
            {
                int tempIndex = crossoverIndex1;
                crossoverIndex1 = crossoverIndex2;
                crossoverIndex2 = tempIndex;
            }

            List<PaintingGridEncoding> childEncodings = new List<PaintingGridEncoding>();
            childEncodings.Add(new PaintingGridEncoding(true));
            childEncodings.Add(new PaintingGridEncoding(true));

            
            for (int i = crossoverIndex1; i <= crossoverIndex2; i++)
            {
                childEncodings[0].AddPaintingEncoding(new PaintingEncoding(_paintingEncodings[i]));
                childEncodings[1].AddPaintingEncoding(new PaintingEncoding(other.PaintingEncodingAt(i)));
            }

            childEncodings[0].AddRemainingOrderCrossoverEncodings(other, crossoverIndex1, crossoverIndex2);
            childEncodings[1].AddRemainingOrderCrossoverEncodings(this, crossoverIndex1, crossoverIndex2);

            childEncodings[0].CrossoverRotatedAndFrontVisible(this, other, crossoverIndex1, crossoverIndex2);
            childEncodings[1].CrossoverRotatedAndFrontVisible(other, this, crossoverIndex1, crossoverIndex2);            

            return childEncodings;        
        }


        public List<PaintingGridEncoding> OrderCrossoverModified(PaintingGridEncoding other)
        {

            List<int> crossoverPoints = new List<int>();
            for (int i = 0; i < MainPage.NUMBER_OF_ROWS * MainPage.DIMENSIONS_MULTIPLIER * MainPage.NUMBER_OF_COLUMNS * MainPage.DIMENSIONS_MULTIPLIER; i++)
            {
                crossoverPoints.Add(i);
            }

            while (crossoverPoints.Count > MainPage.NUMBER_OF_ROWS * MainPage.DIMENSIONS_MULTIPLIER * MainPage.NUMBER_OF_COLUMNS * MainPage.DIMENSIONS_MULTIPLIER / 2)
            {
                int removeIndex = MainPage.rand.Next(crossoverPoints.Count);
                crossoverPoints.RemoveAt(removeIndex);
            }

            PaintingEncoding[] paintingArray1 = new PaintingEncoding[MainPage.NUMBER_OF_ROWS * MainPage.DIMENSIONS_MULTIPLIER * MainPage.NUMBER_OF_COLUMNS * MainPage.DIMENSIONS_MULTIPLIER];
            PaintingEncoding[] paintingArray2 = new PaintingEncoding[MainPage.NUMBER_OF_ROWS * MainPage.DIMENSIONS_MULTIPLIER * MainPage.NUMBER_OF_COLUMNS * MainPage.DIMENSIONS_MULTIPLIER];
            List<PaintingEncoding> chosenEncodings1 = new List<PaintingEncoding>();
            List<PaintingEncoding> chosenEncodings2 = new List<PaintingEncoding>();
            foreach (int i in crossoverPoints)
            {
                PaintingEncoding encoding1 = this.PaintingEncodingAt(i);
                paintingArray1[i] = encoding1;
                chosenEncodings1.Add(encoding1);
                
                PaintingEncoding encoding2 = other.PaintingEncodingAt(i);
                paintingArray2[i] = encoding2;
                chosenEncodings2.Add(encoding2);
            }

            List<PaintingGridEncoding> childEncodings = new List<PaintingGridEncoding>();
            childEncodings.Add(new PaintingGridEncoding(true));
            childEncodings.Add(new PaintingGridEncoding(true));

            int thisIndex = 0;
            int otherIndex = 0;
            for (int i = 0; i < MainPage.NUMBER_OF_ROWS * MainPage.DIMENSIONS_MULTIPLIER * MainPage.NUMBER_OF_COLUMNS * MainPage.DIMENSIONS_MULTIPLIER; i++)
            {
                

                if (paintingArray1[i] == null)
                {
                    PaintingEncoding encoding1 = null;
                    bool found = false;
                    while (!found)
                    {
                        encoding1 = this.PaintingEncodingAt(thisIndex);
                        bool alreadyPresent = false;
                        for (int j = 0; j < chosenEncodings1.Count; j++)
                        {
                            PaintingEncoding encoding = chosenEncodings1[j];
                            if (encoding.PaintingIndex == encoding1.PaintingIndex)
                            {
                                alreadyPresent = true;
                                break;
                            }
                        }
                        if (alreadyPresent)
                        {
                            thisIndex++;
                        }
                        else
                        {
                            found = true;
                        }
                    }
                    paintingArray1[i] = encoding1;
                    chosenEncodings1.Add(encoding1);
                }

                if (paintingArray2[i] == null)
                {
                    PaintingEncoding encoding2 = null;
                    bool found = false;
                    while(!found)
                    {
                        encoding2 = other.PaintingEncodingAt(otherIndex);
                        bool alreadyPresent = false;
                        for (int j = 0; j < chosenEncodings2.Count; j++)
                        {
                            PaintingEncoding encoding = chosenEncodings2[j];
                            if (encoding.PaintingIndex == encoding2.PaintingIndex)
                            {
                                alreadyPresent = true;
                                break;
                            }
                        }
                        if (alreadyPresent)
                        {
                            otherIndex++;
                        }
                        else
                        {
                            found = true;
                        }
                    }
                    paintingArray2[i] = encoding2;
                    chosenEncodings2.Add(encoding2);
                }
            }

            for (int i = 0; i < MainPage.NUMBER_OF_ROWS * MainPage.DIMENSIONS_MULTIPLIER * MainPage.NUMBER_OF_COLUMNS * MainPage.DIMENSIONS_MULTIPLIER; i++)
            {
                PaintingEncoding paintingEncoding1 = paintingArray1[i];
                childEncodings[0].AddPaintingEncoding(new PaintingEncoding(paintingEncoding1));
                PaintingEncoding paintingEncoding2 = paintingArray2[i];
                childEncodings[1].AddPaintingEncoding(new PaintingEncoding(paintingEncoding2));
            }


            childEncodings[0].CrossoverRotatedAndFrontVisibleModified(this, other);
            childEncodings[1].CrossoverRotatedAndFrontVisibleModified(this, other);

            return childEncodings;
        }

        public void Mutate(double mutationRate)
        {
            foreach (PaintingEncoding paintingEncoding in _paintingEncodings)
            {
                if (MainPage.rand.NextDouble() < mutationRate)
                {
                    int newPaintingIndex = MainPage.rand.Next(MainPage.NUMBER_OF_ROWS * MainPage.DIMENSIONS_MULTIPLIER * MainPage.NUMBER_OF_COLUMNS * MainPage.DIMENSIONS_MULTIPLIER);
                    PaintingEncoding swapEncoding = FindPaintingEncoding(newPaintingIndex);
                    swapEncoding.PaintingIndex = paintingEncoding.PaintingIndex;
                    paintingEncoding.PaintingIndex = newPaintingIndex;
                    paintingEncoding.Mutate(mutationRate);
                    
                }
            }
        }

        public void AddPaintingEncoding(PaintingEncoding paintingEncoding)
        {
            _paintingEncodings.Add(paintingEncoding);
        }

        public void InsertPaintingEncoding(int index, PaintingEncoding paintingEncoding)
        {
            _paintingEncodings.Insert(index, paintingEncoding);
        }

        public PaintingEncoding FindPaintingEncoding(int paintingIndex)
        {
            foreach (PaintingEncoding paintingEncoding in _paintingEncodings)
            {
                if (paintingIndex == paintingEncoding.PaintingIndex)
                {
                    return paintingEncoding;
                }
            }

            return null;
        }

        public XElement ToXml()
        {
            XElement gridEncodingElement = new XElement("PaintingGridEncoding");

            XElement paintingChoiceElement = new XElement("PaintingSelection");
            XText paintingChoiceText = new XText(App.PaintingChoice.ToString());
            paintingChoiceElement.Add(paintingChoiceText);
            gridEncodingElement.Add(paintingChoiceElement);

            XElement multiplierElement = new XElement("DimensionMultiplier");
            XText multiplierText = new XText(MainPage.DIMENSIONS_MULTIPLIER.ToString());
            paintingChoiceElement.Add(multiplierText);
            gridEncodingElement.Add(multiplierElement);

            foreach (PaintingEncoding paintingEncoding in _paintingEncodings)
            {
                gridEncodingElement.Add(paintingEncoding.ToXml());
            }

            return gridEncodingElement;
        }
       
    }
}
