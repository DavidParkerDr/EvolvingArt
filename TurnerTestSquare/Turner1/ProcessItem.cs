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
using System.ComponentModel;

namespace Turner1
{
    public class ProcessItem : INotifyPropertyChanged
    {
        private int _generation = 0;
        public int Generation
        {
            get
            {
                return _generation;
            }
            set
            {
                if (_generation != value)
                {
                    _generation = value;
                    RaisePropertyChanged("Generation");
                }
            }
        }

        private double _progress = 0;
        public double Progress
        {
            get
            {
                return _progress;
            }
            set
            {
                if (_progress != value)
                {
                    _progress = value;
                    RaisePropertyChanged("Progress");
                }
            }
        }

        private string _phase = "Initialise";
        public string Phase
        {
            get
            {
                return _phase;
            }
            set
            {
                if (_phase != value)
                {
                    _phase = value;
                    RaisePropertyChanged("Phase");
                }
            }
        }

        
	
		private Individual _fittestIndividual = null;
		public Individual FittestIndividual 
		{
			get
			{
				return _fittestIndividual;
			}
			set
			{
				if (_fittestIndividual != value)
				{
					_fittestIndividual = value;
					RaisePropertyChanged("FittestIndividual");
				}
			}
		}
        

        private bool _isComplete = false;
        public bool IsComplete
        {
            get
            {
                return _isComplete;
            }
            set
            {
                if (_isComplete != value)
                {
                    _isComplete = value;
                    RaisePropertyChanged("IsComplete");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
            }
        }
    }
}
