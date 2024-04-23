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
    public class GeneticAlgorithm
    {
        List<Individual> _population;

        // delegate declaration 
        public delegate void GenerationCompleteHandler(object sender, GenerationCompleteEventArgs gcea);
        public delegate void OptimisationCompleteHandler(object sender, OptimisationCompleteEventArgs ocea);
        // event declaration 
        public event GenerationCompleteHandler GenerationComplete;
        public event OptimisationCompleteHandler OptimisationComplete;

        public event Action<ProcessItem> WorkCompleted;
        public event Action<ProcessItem> WorkProgress;

        public List<ImageHolder> Images
        {
            get;
            set;
        }
        int _populationSize = 40;
        public int PopulationSize
        {
            get
            {
                return _populationSize;
            }
            set
            {
                _populationSize = value;
                
            }
        }

        public int PaintingChoice
        {
            get;
            set;
        }

        public bool PauseOrdered
        {
            get;
            set;
        }

        public int FitnessType
        {
            get;
            set;
        }

        public double MutationRate
        {
            get;
            set;
        }
        public double CrossoverRate
        {
            get;
            set;
        }

        public int MaxGenerations
        {
            get;
            set;
        }

        public int MillisecondsPerGeneration
        {
            get;
            set;
        }

        public int Generation
        { 
            get; 
            set; 
        }

        public int EdgeThickness
        {
            get;
            set;
        }

        public GeneticAlgorithm()
        {
            _population = new List<Individual>();
            Images = new List<ImageHolder>();
            LoadImages();
            Generation = 0;
            FitnessType = 1;
            EdgeThickness = 1;
            MutationRate = 0.05;
            CrossoverRate = 0.7;
            PopulationSize = 40;
            MaxGenerations = 100;
            PaintingChoice = App.PaintingChoice;
            PauseOrdered = false;
            MillisecondsPerGeneration = 0;

        }

        public GeneticAlgorithm(XElement gaElement)
        {
            _population = new List<Individual>();
            XElement paintingChoice = gaElement.Element("PaintingSelection");
            App.PaintingChoice = int.Parse(paintingChoice.Value);
            PaintingChoice = App.PaintingChoice;
            Images = new List<ImageHolder>();
            LoadImages(); 
            Generation = int.Parse(gaElement.Element("Generation").Value);
            MutationRate = double.Parse(gaElement.Element("MutationRate").Value);
            CrossoverRate = double.Parse(gaElement.Element("CrossoverRate").Value);
            PopulationSize = int.Parse(gaElement.Element("PopulationSize").Value);
            FitnessType = int.Parse(gaElement.Element("FitnessType").Value);
            if (FitnessType == 1)
            {
                EdgeThickness = int.Parse(gaElement.Element("EdgeThickness").Value);
            }
            MaxGenerations = int.Parse(gaElement.Element("MaxGenerations").Value);
            PauseOrdered = false;
            foreach (XElement individualEncoding in gaElement.Element("Population").Elements("Individual"))
            {
                _population.Add(new Individual(this, individualEncoding));
            }


        }

        public Individual GetBestIndividual()
        {
            return _population[0];
        }

        private void LoadImages()
        {
            for (int i = 0; i < MainPage.NUMBER_OF_PAINTINGS; i++)
            {
                Uri frontImageUri = new Uri(string.Format("/Turner1;component/Images/Roberto{0}/{1}a.png", App.PaintingChoice, (i + 1).ToString()), UriKind.Relative);
                Uri backImageUri = new Uri(string.Format("/Turner1;component/Images/Roberto{0}/{1}a.png", App.PaintingChoice, (i + 1).ToString()), UriKind.Relative);
                ImageHolder imageHolder = new ImageHolder(frontImageUri, backImageUri);
                Images.Add(imageHolder);
            }
        }
        public List<Pixel> GetAllPixels(PaintingEncoding encoding)
        {
            ImageHolder image = Images[encoding.PaintingIndex];
            return image.GetAllPixels(encoding.Rotated, encoding.FrontVisible);


        }

        public List<Pixel> GetLeftEdgePixels(PaintingEncoding encoding)
        {
            ImageHolder image = Images[encoding.PaintingIndex];
            return image.GetLeftEdgePixels(encoding.Rotated, encoding.FrontVisible, EdgeThickness);


        }

        public List<Pixel> GetRightEdgePixels(PaintingEncoding encoding)
        {
            ImageHolder image = Images[encoding.PaintingIndex];
            return image.GetRightEdgePixels(encoding.Rotated, encoding.FrontVisible, EdgeThickness);


        }

        public List<Pixel> GetTopEdgePixels(PaintingEncoding encoding)
        {
            ImageHolder image = Images[encoding.PaintingIndex];
            return image.GetTopEdgePixels(encoding.Rotated, encoding.FrontVisible, EdgeThickness);


        }

        public List<Pixel> GetBottomEdgePixels(PaintingEncoding encoding)
        {
            ImageHolder image = Images[encoding.PaintingIndex];
            return image.GetBottomEdgePixels(encoding.Rotated, encoding.FrontVisible, EdgeThickness);


        }

        

        public void SortPopulation()
        {
            _population.Sort();
        }

        public Individual SelectParent()
        {
            double rootPopulationSize = Math.Sqrt(PopulationSize);
            double randomSelection = MainPage.rand.NextDouble() * rootPopulationSize;
            double selectedIndex = Math.Pow(randomSelection, 2);
            int roundedIndex = (int)selectedIndex;
            return _population[roundedIndex];
        }

        public void RaiseGenerationCompleteEvent()
        {
            GenerationCompleteEventArgs gcea = new GenerationCompleteEventArgs(_population[0], Generation);
            GenerationComplete(this, gcea);
        }

        public void RaiseOptimisationCompleteEvent()
        {
            OptimisationCompleteEventArgs ocea = new OptimisationCompleteEventArgs();
            OptimisationComplete(this, ocea);
        }

        public void GenerateChildPopulation()
        {
            List<Individual> newPopulation = new List<Individual>();
            SortPopulation();

            while (newPopulation.Count < PopulationSize)
            {
                Individual parent1 = SelectParent();
                Individual parent2 = parent1;
                while (parent1 == parent2)
                {
                    parent2 = SelectParent();
                }

                List<PaintingGridEncoding> childEncodings = null;
                if (MainPage.rand.NextDouble() < CrossoverRate)
                {
                    childEncodings = parent1.Encoding.Crossover(parent2.Encoding);
                }
                else
                {
                    childEncodings = new List<PaintingGridEncoding>();
                    childEncodings.Add(new PaintingGridEncoding(parent1.Encoding));
                    childEncodings.Add(new PaintingGridEncoding(parent2.Encoding));
                }
                foreach (PaintingGridEncoding childEncoding in childEncodings)
                {
                    childEncoding.Mutate(MutationRate);
                    Individual child = new Individual(this, childEncoding);
                    newPopulation.Add(child);
                }

            }

            foreach (Individual child in newPopulation)
            {
                _population.Add(child);
            }
            _population.Sort();
        }

        public void CropPopulation()
        {
            while (_population.Count > PopulationSize)
            {
                _population.RemoveRange(PopulationSize, _population.Count - PopulationSize);
            }
        }

        public void IntitialisePopulation()
        {
            while (_population.Count < PopulationSize)
            {
                _population.Add(new Individual(this));
                WorkProgress(new ProcessItem()
                {
                    Generation = this.Generation,
                    Progress = (double)this._population.Count / (double)this.PopulationSize * 100,
                    Phase = "Initialise",
                    FittestIndividual = null,
                    IsComplete = false
                });

            }
            SortPopulation();
            WorkProgress(new ProcessItem()
            {
                Generation = this.Generation,
                Progress = (double)this._population.Count / (double)this.PopulationSize * 100,
                Phase = "Initialise",
                FittestIndividual = this._population[0],
                IsComplete = true
            });

        }

        public void Evolve()
        {
            if (_populationSize > _population.Count)
            {
                IntitialisePopulation();
            }
            else
            {
                CropPopulation();
            }
            WorkProgress(new ProcessItem()
            {
                Generation = this.Generation,
                Progress = (double)this.Generation / (double)this.MaxGenerations * 100,
                Phase = "Evolution",
                FittestIndividual = this._population[0],
                IsComplete = false
            });
            while (Generation < MaxGenerations && !PauseOrdered)
            {
                EvolveGeneration();
            }

            PauseOrdered = false;

            WorkCompleted(new ProcessItem()
            {
                Generation = this.Generation,
                Progress = (double)this.Generation / (double)this.MaxGenerations * 100,
                Phase = "Evolution",
                FittestIndividual = this._population[0],
                IsComplete = true
            });
        }

        public void EvolveGeneration()
        {
            DateTime generationStartTime = DateTime.Now;
            Generation++;
            GenerateChildPopulation();
            CropPopulation();
            if (MillisecondsPerGeneration > 0)
            {
                DateTime generationEndTime = DateTime.Now;
                TimeSpan generationTimeDifference = generationEndTime - generationStartTime;
                while (generationTimeDifference.TotalMilliseconds < MillisecondsPerGeneration)
                {
                    generationEndTime = DateTime.Now;
                    generationTimeDifference = generationEndTime - generationStartTime;
                }
            }
            // raise generation complete event
            WorkProgress(new ProcessItem()
            {
                Generation = this.Generation,
                Progress = (double)this.Generation / (double)this.MaxGenerations * 100,
                Phase = "Evolution",
                FittestIndividual = this._population[0],
                IsComplete = false
            });

        }
        public XElement ToXml()
        {
            XElement gaElement = new XElement("GeneticAlgorithm");

            XElement paintingChoiceElement = new XElement("PaintingSelection");
            XText paintingChoiceText = new XText(App.PaintingChoice.ToString());
            paintingChoiceElement.Add(paintingChoiceText);
            gaElement.Add(paintingChoiceElement);

            XElement maxGenerationElement = new XElement("MaxGenerations");
            XText maxGenerationText = new XText(MaxGenerations.ToString());
            maxGenerationElement.Add(maxGenerationText);
            gaElement.Add(maxGenerationElement);

            XElement generationElement = new XElement("Generation");
            XText generationText = new XText(Generation.ToString());
            generationElement.Add(generationText);
            gaElement.Add(generationElement);

            XElement populationSizeElement = new XElement("PopulationSize");
            XText populationSizeText = new XText(PopulationSize.ToString());
            populationSizeElement.Add(populationSizeText);
            gaElement.Add(populationSizeElement);

            XElement mutationRateElement = new XElement("MutationRate");
            XText mutationRateText = new XText(MutationRate.ToString());
            mutationRateElement.Add(mutationRateText);
            gaElement.Add(mutationRateElement);

            XElement crossoverRateElement = new XElement("CrossoverRate");
            XText crossoverRateText = new XText(CrossoverRate.ToString());
            crossoverRateElement.Add(crossoverRateText);
            gaElement.Add(crossoverRateElement);

            XElement fitnessTypeElement = new XElement("FitnessType");
            XText fitnessTypeText = new XText(FitnessType.ToString());
            fitnessTypeElement.Add(fitnessTypeText);
            gaElement.Add(fitnessTypeElement);

            if (FitnessType == 1)
            {
                XElement edgeThicknessElement = new XElement("EdgeThickness");
                XText edgeThicknessText = new XText(EdgeThickness.ToString());
                edgeThicknessElement.Add(edgeThicknessText);
                gaElement.Add(edgeThicknessElement);
            }

            XElement populationElement = new XElement("Population");
            foreach (Individual individual in _population)
            {
                populationElement.Add(individual.ToXml());
            }

            gaElement.Add(populationElement);
            return gaElement;
        }
        
    }
}
