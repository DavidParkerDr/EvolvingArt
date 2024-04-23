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
                for (int i = 0; i < MainPage.NUMBER_OF_PAINTINGS; i++)
                {
                    unPlaced.Add(i);
                }

                int row = -1;
                int col = 0;

                while (unPlaced.Count > 0)
                {
                    if (col % MainPage.NUMBER_OF_COLUMNS == 0)
                    {
                        row++;
                        col = 0;
                    }

                    int index = MainPage.rand.Next(unPlaced.Count);
                    int paintingIndex = unPlaced[index];
                    unPlaced.RemoveAt(index);

                    bool rotated = true;
                    if (MainPage.FlipCoin())
                    {
                        rotated = false;
                    }

                    bool frontVisible = true;
                    if (MainPage.FlipCoin())
                    {
                        frontVisible = false;
                    }

                    _paintingEncodings.Add(new PaintingEncoding(paintingIndex, rotated, frontVisible));

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

                    paintingEncoding.Rotated = otherPaintingEncoding.Rotated;
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

                paintingEncoding.Rotated = otherPaintingEncoding.Rotated;

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
            

            int crossoverIndex1 = MainPage.rand.Next(MainPage.NUMBER_OF_PAINTINGS);
            int crossoverIndex2 = MainPage.rand.Next(MainPage.NUMBER_OF_PAINTINGS);

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

        private PaintingGridEncoding GenerateChildFromCrossover(PaintingGridEncoding other, bool useThis, List<int> crossoverPoints, List<int> remainingPoints, List<PaintingEncoding> freeEncodings)
        {
            PaintingGridEncoding source = this;
            PaintingGridEncoding otherSource = other;
            if (!useThis) 
            {
                source = other;
                otherSource = this;
            }
            PaintingGridEncoding child = new PaintingGridEncoding(true);
            // create an empty array ready to take painting encodings, one for 'this' and one for 'other'
            PaintingEncoding[] paintingArray = new PaintingEncoding[MainPage.NUMBER_OF_PAINTINGS];
            // for all of the selected crossover points
            foreach (int i in crossoverPoints)
            {
                // find the painting encoding at the crossover point of 'this' painting grid
                PaintingEncoding encoding = new PaintingEncoding(source.PaintingEncodingAt(i));
                // add it to the array at the correct index
                paintingArray[i] = encoding;
                // it has been assigned, remove it from the free list
                //find freeEncoding with correct painting index
                for (int j = 0; j < freeEncodings.Count; j++)
                {
                    if (freeEncodings[j].PaintingIndex == encoding.PaintingIndex)
                    {
                        freeEncodings.RemoveAt(j);
                        break;
                    }
                }
            }

            // go through each painting slot in the grid
            for (int i = 0; i < remainingPoints.Count; )
            {
                int slotIndex = remainingPoints[i];                
                // get the encoding from 'other'
                PaintingEncoding encodingFromOther = otherSource.PaintingEncodingAt(slotIndex);
                int otherPaintingIndex = encodingFromOther.PaintingIndex;
                bool assigned = false;
                // check all the already assigned encodings
                for (int j = 0; j < freeEncodings.Count; )
                {
                    PaintingEncoding encoding = freeEncodings[j];
                    if (encoding.PaintingIndex == otherPaintingIndex)
                    {
                        paintingArray[slotIndex] = encoding;
                        freeEncodings.Remove(encoding);
                        remainingPoints.Remove(slotIndex);
                        assigned = true;
                        break;
                    }
                    else
                    {
                        j++;
                    }
                }
                // if 'other' encoding does not conflict then assign it in the slot
                if (!assigned)
                {
                    i++;
                }
            }
            // at this point we should only have conflicting slots unassigned and in the remaining points list
            foreach (int point in remainingPoints)
            {
                // choose a position to remove
                int contentIndex = MainPage.rand.Next(freeEncodings.Count);
                PaintingEncoding freeEncoding = freeEncodings[contentIndex];
                // add the index at that position to the remaining points
                freeEncodings.Remove(freeEncoding);
                paintingArray[point] = freeEncoding;

            }
            // move the painting encodings in to the full grid encoding from the temp array
            for (int i = 0; i < MainPage.NUMBER_OF_PAINTINGS; i++ )
            {
                PaintingEncoding paintingEncoding = paintingArray[i];
                child.AddPaintingEncoding(new PaintingEncoding(paintingEncoding));
            }
            return child;
        }

        


        public List<PaintingGridEncoding> OrderCrossoverModified(PaintingGridEncoding other)
        {
            

            // create a list of all possible crossover points
            // this is all the slot indexes in the painting grid
            List<int> crossoverPoints = new List<int>();
            // create a list of all remaining points
            // this is all the slot indexes in the painting grid
            List<int> remainingPoints1 = new List<int>();
            List<int> remainingPoints2 = new List<int>();
            // create an emtpy list ready to take painting encodings, one for 'this' and one for 'other'
            // the list will contain painting indexes that can still be assigned
            List<PaintingEncoding> freeEncodings1 = new List<PaintingEncoding>();
            List<PaintingEncoding> freeEncodings2 = new List<PaintingEncoding>();
            for (int i = 0; i < MainPage.NUMBER_OF_PAINTINGS; i++)
            {
                crossoverPoints.Add(i);
                freeEncodings1.Add(new PaintingEncoding(other.PaintingEncodingAt(i)));
                freeEncodings2.Add(new PaintingEncoding(this.PaintingEncodingAt(i)));
            }
            // randomly remove half of the possible crossover points
            while (crossoverPoints.Count > MainPage.NUMBER_OF_PAINTINGS / 2)
            {
                // choose a position to remove
                int removeIndex = MainPage.rand.Next(crossoverPoints.Count);
                // add the index at that position to the remaining points
                remainingPoints1.Add(crossoverPoints[removeIndex]);
                remainingPoints2.Add(crossoverPoints[removeIndex]);
                // remove it from the crossover points
                crossoverPoints.RemoveAt(removeIndex);
                
            }
            PaintingGridEncoding child1 = GenerateChildFromCrossover(other, true, crossoverPoints, remainingPoints1, freeEncodings1);
            PaintingGridEncoding child2 = GenerateChildFromCrossover(other, false, crossoverPoints, remainingPoints2, freeEncodings2);
            // prepare an empty list that will be populated with the crossed over children.
            List<PaintingGridEncoding> childEncodings = new List<PaintingGridEncoding>();
            // add two empty painting grid encodings to the list
            childEncodings.Add(child1);
            childEncodings.Add(child2);
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
                    int newPaintingIndex = MainPage.rand.Next(MainPage.NUMBER_OF_PAINTINGS);
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

            foreach (PaintingEncoding paintingEncoding in _paintingEncodings)
            {
                gridEncodingElement.Add(paintingEncoding.ToXml());
            }

            return gridEncodingElement;
        }
       
    }
}
