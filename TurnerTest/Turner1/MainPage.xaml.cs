using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Windows.Threading;
using System.IO;
using System.Xml.Linq;

namespace Turner1
{
    public partial class MainPage : UserControl
    {
        PaintingGrid _paintingGrid = new PaintingGrid();
        public static Random rand = new Random();
        public static int NUMBER_OF_PAINTINGS = 12;
        public static int NUMBER_OF_ROWS = 3;
        public static int NUMBER_OF_COLUMNS = 4;

        private Dispatcher currentDispatcher;

        GeneticAlgorithm ga = null;

        public MainPage()
        {
           
            InitializeComponent();
            
            currentDispatcher = this.LayoutRoot.Dispatcher;
            InnerLayoutRoot.Children.Insert(0, _paintingGrid);
            SaveCurrentGAButton.Visibility = Visibility.Collapsed;
            OptimisationProgress.Visibility = Visibility.Collapsed;
            ResetOptimiseButton.Visibility = Visibility.Collapsed;
            PhaseTextBox.Visibility = Visibility.Collapsed;
            GenerationTextBox.Visibility = Visibility.Collapsed;
            FitnessTextBox.Visibility = Visibility.Collapsed;
            AddHandler(KeyDownEvent, new KeyEventHandler(GlobalKeyDownHandler), true);
            ToggleSlideShow();
            
        }

        private void Generation_Complete(ProcessItem token)
        {
            if (token.FittestIndividual != null)
            {
                _paintingGrid.Configure(token.FittestIndividual.Encoding, false);
            }

            // update progress bar
            OptimisationProgress.Value = token.Progress;
            PhaseTextBox.Text = "Phase: " + token.Phase;
            GenerationTextBox.Text = "Generation: " + token.Generation.ToString() + "/" + ga.MaxGenerations;
            if (token.FittestIndividual == null)
            {
                FitnessTextBox.Text = "Fitness: Still initialising";
            }
            else
            {
                FitnessTextBox.Text = "Fitness: " + token.FittestIndividual.Fitness.ToString();
            }

        }

        private void Optimisation_Complete(ProcessItem token)
        {
            _paintingGrid.Configure(token.FittestIndividual.Encoding, false);
            OptimisationProgress.Visibility = Visibility.Collapsed;
            PhaseTextBox.Visibility = Visibility.Collapsed;
            OptimiseButton.Content = "Continue Optimisation";
            OptimiseButton.Visibility = Visibility.Visible;
            ResetOptimiseButton.Visibility = Visibility.Visible;
            SaveCurrentGAButton.Visibility = Visibility.Visible;
            LoadPreviousGAButton.Visibility = Visibility.Visible;
            _paintingGrid.ManualMode = true;
            GenerationTextBox.Text = "Generation: " + token.Generation.ToString() + "/" + ga.MaxGenerations;
            if (token.FittestIndividual == null)
            {
                FitnessTextBox.Text = "Fitness: Still initialising";
            }
            else
            {
                FitnessTextBox.Text = "Fitness: " + token.FittestIndividual.Fitness.ToString();
            }
        }

        private void AddToken(ProcessItem token)
        {
            //_paintingGrid.Configure(token.FittestIndividual.Encoding);

            // update progress bar
            double progressValue = (double)token.Generation / (double)ga.MaxGenerations;
            OptimisationProgress.Value = progressValue * 100;
        }

        public void StartProcess()
        {
            // Events used during background processing
            DoWorkEventHandler workHandler = null;
            RunWorkerCompletedEventHandler doneHandler = null;
            Action<ProcessItem> gaProgress = null;
            Action<ProcessItem> gaCompleted = null;
            
            // Implementation of the BackgroundWorker
            var wrkr = new BackgroundWorker();
            wrkr.DoWork += workHandler =
                delegate(object oDoWrk, DoWorkEventArgs eWrk)
                {
                    // Unwire the workHandler to prevent memory leaks
                    wrkr.DoWork -= workHandler;
                    ga.WorkProgress += gaProgress =
                        delegate(ProcessItem result)
                        {
                            // Call the method on the UI thread so that we can get 
                            // updates and avoid cross-threading exceptions.
                            currentDispatcher.BeginInvoke(
                                new Action<ProcessItem>(Generation_Complete), result);
                        };
                    ga.WorkCompleted += gaCompleted =
                        delegate(ProcessItem result)
                        {
                            // Unwire all events for this instance 
                            // of the LongRunningObject
                            ga.WorkProgress -= gaProgress;
                            ga.WorkCompleted -= gaCompleted;
                            currentDispatcher.BeginInvoke(
                                new Action<ProcessItem>(Optimisation_Complete), result);
                        };
                    // Events are wired for the business object, 
                    // this where we start the actual work.
                    

                    
                    
                    ga.Evolve();
                };
            
            // This is where the actual asynchronous process will 
            // start performing the work that is wired up in the 
            // previous statements.
            wrkr.RunWorkerAsync();
            
        }

        public static int flipTest = 0;

        public static bool FlipCoin()
        {
            double randomDouble = MainPage.rand.NextDouble();
            if (randomDouble < 0.5)
            {
                MainPage.flipTest++;
                return true;
            }
            else
            {
                MainPage.flipTest--;
                return false;
            }
        }


        private void RandomiseButton_Click(object sender, RoutedEventArgs e)
        {
            _paintingGrid.Randomise();
        }

        private void ToggleShowButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleSlideShow();
        }

        private void ToggleSlideShow()
        {
            if (ToggleShowButton.Content.ToString() == "Start Random Slideshow")
            {
                ToggleShowButton.Content = "Stop Random Slideshow";
            }
            else
            {

                ToggleShowButton.Content = "Start Random Slideshow";

            }
            _paintingGrid.ToggleTimer();
        }

        private void OptimiseButton_Click(object sender, RoutedEventArgs e)
        {
            if (OptimiseButton.Content.ToString() != "Pause Optimisation")
            {
                // add parameters to GA from boxes
                if (ga == null)
                {
                    ga = new GeneticAlgorithm();
                    ga.MillisecondsPerGeneration = (int)GetTimerInterval() * 1000;
                    // ga.GenerationComplete += new GeneticAlgorithm.GenerationCompleteHandler(Generation_Complete);
                    // ga.OptimisationComplete += new GeneticAlgorithm.OptimisationCompleteHandler(Optimisation_Complete);
                }
                if (ga.Generation < ga.MaxGenerations)
                {

                    ga.PaintingChoice = App.PaintingChoice;
                    ga.FitnessType = FitnessComboBox.SelectedIndex;
                    ga.EdgeThickness = int.Parse(EdgeThicknessTextBox.Text);
                    ga.MaxGenerations = int.Parse(MaxGenTextBox.Text);
                    ga.PopulationSize = int.Parse(PopSizeTextBox.Text);
                    ga.MutationRate = double.Parse(MutationTextBox.Text);
                    ga.CrossoverRate = double.Parse(CrossoverTextBox.Text);
                    ga.MillisecondsPerGeneration = (int)GetTimerInterval() * 1000;
                    OptimiseButton.Content = "Pause Optimisation";
                    // OptimiseButton.Visibility = Visibility.Collapsed;
                    ResetOptimiseButton.Visibility = Visibility.Collapsed;
                    SaveCurrentGAButton.Visibility = Visibility.Collapsed;
                    LoadPreviousGAButton.Visibility = Visibility.Collapsed;
                    PhaseTextBox.Visibility = Visibility.Visible;
                    OptimisationProgress.Visibility = Visibility.Visible;
                    GenerationTextBox.Visibility = Visibility.Visible;
                    FitnessTextBox.Visibility = Visibility.Visible;
                    _paintingGrid.ManualMode = false;
                    _paintingGrid.PlayOrContinueBackgroundMusic();
                    //ga.Evolve();
                    StartProcess();
                }
                else
                {
                    MessageBox.Show("Maximum generation reached, please increase limit in the settings or reset optimisation.");
                }
            }
            else
            {
                //_paintingGrid.PlayOrContinueBackgroundMusic();
                OptimiseButton.Content = "Continue Optimisation";
                ga.PauseOrdered = true;
                OptimiseButton.Visibility = Visibility.Visible;
                ResetOptimiseButton.Visibility = Visibility.Visible;
                SaveCurrentGAButton.Visibility = Visibility.Visible;
                LoadPreviousGAButton.Visibility = Visibility.Visible;
                _paintingGrid.ManualMode = true;
                _paintingGrid.StopBackgroundMusic();
            }
            
            
            
        }

        private void ResetOptimiseButton_Click(object sender, RoutedEventArgs e)
        {


            ResetOptimisation();

        }

        private void ResetOptimisation()
        {
            ga = null;
            //  ga.GenerationComplete += new GeneticAlgorithm.GenerationCompleteHandler(Generation_Complete);
            //  ga.OptimisationComplete += new GeneticAlgorithm.OptimisationCompleteHandler(Optimisation_Complete);
            OptimiseButton.Content = "Start Optimisation";
            OptimiseButton.Visibility = Visibility.Visible;
            OptimisationProgress.Visibility = Visibility.Collapsed;
            PhaseTextBox.Visibility = Visibility.Collapsed;
            GenerationTextBox.Visibility = Visibility.Collapsed;
            FitnessTextBox.Visibility = Visibility.Collapsed;
            ResetOptimiseButton.Visibility = Visibility.Collapsed;
            SaveCurrentGAButton.Visibility = Visibility.Collapsed;
            LoadPreviousGAButton.Visibility = Visibility.Visible;
        }

        private void LoadDefaultsButton_Click(object sender, RoutedEventArgs e)
        {
            PaintingSelectionCombo.SelectedIndex = 0;
            SlideshowDurationCombo.SelectedIndex = 5;
            FitnessComboBox.SelectedIndex = 1;
            FitnessPanel.Visibility = Visibility.Visible;
            FitnessTextBox.Text = "1";
            CrossoverTextBox.Text = "0.7";
            MutationTextBox.Text = "0.05";
            PopSizeTextBox.Text = "40";
            MaxGenTextBox.Text = "100";



        }

        private void AnalyseButton_Click(object sender, RoutedEventArgs e)
        {
            GeneticAlgorithm tempGA = new GeneticAlgorithm();

            tempGA.FitnessType = FitnessComboBox.SelectedIndex;
            tempGA.EdgeThickness = int.Parse(EdgeThicknessTextBox.Text);
            tempGA.MaxGenerations = int.Parse(MaxGenTextBox.Text);
            tempGA.PopulationSize = int.Parse(PopSizeTextBox.Text);
            tempGA.MutationRate = double.Parse(MutationTextBox.Text);
            tempGA.CrossoverRate = double.Parse(CrossoverTextBox.Text);

            PaintingGridEncoding gridEncoding = _paintingGrid.GenerateEncoding();
            Individual ind = new Individual(tempGA, gridEncoding);

            MessageBox.Show("Fitness: " + ind.Fitness.ToString());


        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Count > 0)
            {
                if (((TabItem)e.RemovedItems[0]).Header.ToString() == "Manual")
                {
                    if (_paintingGrid.GetTimerOn())
                    {
                        _paintingGrid.ToggleTimer();
                        ToggleShowButton.Content = "Start Random Slideshow";
                    }
                }
                if (((TabItem)e.AddedItems[0]).Header.ToString() == "Optimise")
                {
                    if (ga != null)
                    {
                        if (ga.PaintingChoice != App.PaintingChoice)
                        {
                            ResetOptimisation();
                        }
                    }
                }
            }
        }

        private void SlideshowDurationComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            double seconds = 9.6;
            if (e.AddedItems.Count > 0)
            {
                string selection = ((ComboBoxItem)e.AddedItems[0]).Content.ToString();
                
             seconds = ConvertDurationStringToDouble(selection);
                
                
            }
            if (ga != null)
            {
                ga.MillisecondsPerGeneration = (int)seconds * 1000;
            }
            
            _paintingGrid.SetTimerInterval(seconds);
        }
         private void PaintingSelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
         {
            if (e.AddedItems.Count > 0)
            {
                string selection = ((ComboBoxItem)e.AddedItems[0]).Content.ToString();
                
                if (selection == "Roberto 1")
                {
                    App.PaintingChoice = 1;
                }
                else if (selection == "Roberto 2")
                {
                    App.PaintingChoice = 2;
                }
                else if (selection == "Roberto 3")
                {
                    App.PaintingChoice = 3;
                }
                else if (selection == "Roberto 4")
                {
                    App.PaintingChoice = 4;
                }
               
                if (InnerLayoutRoot != null)
                {
                    InnerLayoutRoot.Children.Remove(_paintingGrid);
                    _paintingGrid = new PaintingGrid();
                    _paintingGrid.SetTimerInterval(GetTimerInterval());
                    InnerLayoutRoot.Children.Insert(0, _paintingGrid);
                    
                }
                
            }
            
            
        }

         public double ConvertDurationStringToDouble(string selection)
         {
             
             if (selection == "1 second")
             {
                 return 1;
             }
             else if (selection == "2 seconds")
             {
                 return  2;
             }
             else if (selection == "3 seconds")
             {
                 return  3;
             }
             else if (selection == "4 seconds")
             {
                 return  4;
             }
             else if (selection == "5 seconds")
             {
                 return  5;
             }
             else if (selection == "6 seconds")
             {
                 return  6;
             }
             else if (selection == "9.6 seconds")
             {
                 return  9.6;
             }
             return 9.6;
         }

         public double GetTimerInterval()
         {
             string selection = ((ComboBoxItem)(this.SlideshowDurationCombo.SelectedValue)).Content.ToString();
             return ConvertDurationStringToDouble(selection);
         }

        private void FitnessComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                string selection = ((ComboBoxItem)e.AddedItems[0]).Content.ToString();

                if (selection == "Average distance of all pixels (SLOW)")
                {
                    FitnessPanel.Visibility = Visibility.Collapsed;
                }
                else if (selection == "Average distance of edge pixels")
                {
                    if (FitnessPanel != null)
                    {
                        FitnessPanel.Visibility = Visibility.Visible;
                    }
                }
                

            }

            
        }

        

        private void SaveCurrentButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();

            saveFileDialog.DefaultExt = "xml";
            saveFileDialog.Filter = "XML Files (*.xml)|*.xml|All files (*.*)|*.*";
            saveFileDialog.FilterIndex = 1;

            if (saveFileDialog.ShowDialog() == true)
            {
                using (Stream stream = saveFileDialog.OpenFile())
                {
                    StreamWriter sw = new StreamWriter(stream, System.Text.Encoding.UTF8);
                    sw.Write(_paintingGrid.GenerateEncoding().ToXml().ToString());
                    sw.Close();

                    stream.Close();
                }
            }
        }

        private void LoadPreviousButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.Filter = "XML Files (*.xml)|*.xml|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;

            if (openFileDialog.ShowDialog() == true)
            {
                using (Stream stream = openFileDialog.File.OpenRead())
                {
                    StreamReader sr = new StreamReader(stream, System.Text.Encoding.UTF8);
                    XElement paintingGridEncodingElement = XElement.Parse(sr.ReadToEnd());
                    int previousPaintingChoice = App.PaintingChoice;
                    PaintingGridEncoding encoding = new PaintingGridEncoding(paintingGridEncodingElement);
                    if (previousPaintingChoice != App.PaintingChoice)
                    {
                        PaintingSelectionCombo.SelectedIndex = App.PaintingChoice-1;
                        
                    }
                    _paintingGrid.Configure(encoding, true);

                    sr.Close();

                    stream.Close();
                }
            }
        }

        private void SaveCurrentGAButton_Click(object sender, RoutedEventArgs e)
        {

            if (ga != null)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();

                saveFileDialog.DefaultExt = "xml";
                saveFileDialog.Filter = "XML Files (*.xml)|*.xml|All files (*.*)|*.*";
                saveFileDialog.FilterIndex = 1;

                if (saveFileDialog.ShowDialog() == true)
                {
                    using (Stream stream = saveFileDialog.OpenFile())
                    {
                        StreamWriter sw = new StreamWriter(stream, System.Text.Encoding.UTF8);
                        sw.Write(ga.ToXml().ToString());
                        sw.Close();

                        stream.Close();
                    }
                }
            }
            else
            {
                MessageBox.Show("There is currently no active Genetic Algorithm to save.");
            }
        }

        private void LoadPreviousGAButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.Filter = "XML Files (*.xml)|*.xml|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;

            if (openFileDialog.ShowDialog() == true)
            {
                using (Stream stream = openFileDialog.File.OpenRead())
                {
                    StreamReader sr = new StreamReader(stream, System.Text.Encoding.UTF8);
                    XElement gaElement = XElement.Parse(sr.ReadToEnd());
                    int previousPaintingChoice = App.PaintingChoice;
                    LoadPreviousGA(gaElement);
                    if (previousPaintingChoice != App.PaintingChoice)
                    {
                        PaintingSelectionCombo.SelectedIndex = App.PaintingChoice-1;
                        
                    }
                    _paintingGrid.Configure(ga.GetBestIndividual().Encoding);
                    sr.Close();

                    stream.Close();
                }
            }
        }

        public void LoadPreviousGA(XElement gaElement)
        {
            ga = new GeneticAlgorithm(gaElement);
            
            MaxGenTextBox.Text = ga.MaxGenerations.ToString();
            PopSizeTextBox.Text = ga.PopulationSize.ToString();
            MutationTextBox.Text = ga.MutationRate.ToString();
            CrossoverTextBox.Text = ga.CrossoverRate.ToString();
            GenerationTextBox.Text = "Generation: " + ga.Generation.ToString() + "/" + ga.MaxGenerations;
            GenerationTextBox.Visibility = Visibility.Visible;
            OptimiseButton.Content = "Continue Optimisation";
            FitnessTextBox.Text = "Fitness: " + ga.GetBestIndividual().Fitness.ToString();
            FitnessTextBox.Visibility = Visibility.Visible;
        }

        private void MaxGenTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ga != null)
            {
                ga.MaxGenerations = int.Parse(MaxGenTextBox.Text);
                GenerationTextBox.Text = "Generation: " + ga.Generation.ToString() + "/" + ga.MaxGenerations;
            }
        }

        private void HideUserInterfaceButton_Click(object sender, RoutedEventArgs e)
        {
            UserInterfaceTabs.Visibility = Visibility.Collapsed;
        }

        private void LayoutRoot_MouseMove(object sender, MouseEventArgs e)
        {
           // UserInterfaceTabs.Visibility = Visibility.Visible;
        }

        private void GlobalKeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                UserInterfaceTabs.Visibility = Visibility.Visible;
            }

        }

       
        
    }
}
