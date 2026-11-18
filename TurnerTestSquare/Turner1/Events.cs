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

namespace Turner1
{
    public class GenerationCompleteEventArgs : EventArgs
    {
        public Individual FittestIndividual
        {
            get;
            set;
        }

        public int Generation
        {
            get;
            set;
        }


        public GenerationCompleteEventArgs(Individual fittestIndividual, int generation)
        {
            FittestIndividual = fittestIndividual;
            Generation = generation;
        }

    }

    public class OptimisationCompleteEventArgs : EventArgs
    {
        
        

    }
}
