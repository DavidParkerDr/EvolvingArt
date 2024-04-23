using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Turner1
{
    public partial class PaintingGrid : UserControl
    {
        List<Painting> _paintings = new List<Painting>();
        System.Random RandNum = new System.Random();
        private DispatcherTimer _timer = new DispatcherTimer();
        bool _timerOn = false;

        public bool GetTimerOn()
        {
            return _timerOn;
        }

        public bool ManualMode
        {
            get;
            set;
        }

        public Painting MoveFrom
        {
            get;
            set;
        }

        public Painting MoveTo
        {
            get;
            set;
        }
       
        public bool MoveMode
        {
            get;
            set;
        }

        void _timer_Tick(object sender, EventArgs e)
        {
            Randomise();
        }

        public void ToggleTimer()
        {
            if (_timerOn)
            {
                BackgroundMusic.Stop();
                for (int i = 0; i < _paintings.Count; i++)
                {
                    Painting painting = _paintings[i];
                    painting.StopSounds();
                }
                _timer.Stop();
                _timerOn = false;
                ManualMode = true;
            }
            else
            {
                PlayBackgroundMusic();// BackgroundMusic.Play();
                _timer.Start();
                _timerOn = true;
                ManualMode = false;
            }
        }

        public void SwitchPaintings()
        {
            
            int fromCol = (int)MoveFrom.GetValue(Grid.ColumnProperty);
            int fromRow = (int)MoveFrom.GetValue(Grid.RowProperty);
            int toCol = (int)MoveTo.GetValue(Grid.ColumnProperty);
            int toRow = (int)MoveTo.GetValue(Grid.RowProperty);
            MoveTo.SetValue(Grid.ColumnProperty, fromCol);
            MoveTo.SetValue(Grid.RowProperty, fromRow);
            MoveFrom.SetValue(Grid.ColumnProperty, toCol);
            MoveFrom.SetValue(Grid.RowProperty, toRow);
            MoveTo.PlaySound();
            MoveFrom.PlaySound();
            MoveTo.HideUI();
           // MoveTo.CancelMoveMode();
            MoveTo = null;
            MoveFrom.HideUI();
           // MoveFrom.CancelMoveMode();
            
            MoveFrom = null;
            MoveMode = false;

        }

        public void Configure(PaintingGridEncoding configuration, bool animate = false)
        {
            for (int row = 0; row < 3; row++)
            {
                for (int column = 0; column < 4; column++)
                {

                    PaintingEncoding paintingEncoding = configuration.PaintingEncodingAt(column + (row * 4));
                    Painting painting = _paintings[paintingEncoding.PaintingIndex];
                    painting.Configure(paintingEncoding, animate);                    
                    painting.SetValue(Grid.ColumnProperty, column);
                    painting.SetValue(Grid.RowProperty, row);
                }
            }
            
        }

        public void Randomise()
        {
            PaintingGridEncoding configuration = new PaintingGridEncoding();
            Configure(configuration, true);


            //List<Painting> unPlaced = new List<Painting>();
            //foreach (Painting painting in _paintings)
            //{
            //    unPlaced.Add(painting);
            //}

            //int row = -1;
            //int col = 0;
            
            //while (unPlaced.Count > 0)
            //{
            //    if (col % MainPage.NUMBER_OF_COLUMNS == 0)
            //    {
            //        row++;
            //        col = 0;
            //    }

            //    int index = RandNum.Next(unPlaced.Count);
            //    Painting painting = unPlaced[index];
            //    unPlaced.RemoveAt(index);
            //    painting.SetValue(Grid.ColumnProperty, col);
            //    painting.SetValue(Grid.RowProperty, row);
            //    painting.Randomise();
            //    col++;
            //}
        }

        public PaintingGrid()
        {
            InitializeComponent();
            MoveMode = false;
            LoadPaintings();
            PopulateGrid();
            ManualMode = true;
            SetTimerInterval(6);
            _timer.Tick += new EventHandler(_timer_Tick);
            Randomise();
        }

        public void PlayBackgroundMusic()
        {
            BackgroundMusic.Stop();
            if (BackgroundMusic.Source != null)
            {
                BackgroundMusic.Stop();
                BackgroundMusic.Play();
            }
        }
        public void StopBackgroundMusic()
        {
            BackgroundMusic.Stop();

        }
        public void PlayOrContinueBackgroundMusic()
        {
            //BackgroundMusic.Stop();
            if (BackgroundMusic.Source != null && BackgroundMusic.CurrentState != MediaElementState.Playing)
            {
                BackgroundMusic.Stop();
                BackgroundMusic.Play();
            }
        }

        public void SetTimerInterval(double seconds)
        {
            _timer.Interval = TimeSpan.FromSeconds(seconds);
        }

        public void LoadPaintings()
        {
            if (App.PaintingChoice == 3)
            {
                BackgroundMusic.Stop();
                BackgroundMusic.AutoPlay = true;
                Uri soundSource = new Uri(string.Format("/Turner1;component/Sounds/Roberto{0}/background.mp3", App.PaintingChoice), UriKind.Relative);
                BackgroundMusic.Source = soundSource;
            }
            else
            {
                BackgroundMusic.Stop();
                BackgroundMusic.Source = null;
            }
            _paintings.Clear();
            for (int i = 0; i < MainPage.NUMBER_OF_PAINTINGS; i++)
            {
                Uri frontImageUri = new Uri(string.Format("/Turner1;component/Images/Roberto{0}/{1}a.png", App.PaintingChoice, (i + 1).ToString()), UriKind.Relative);
                Uri backImageUri = new Uri(string.Format("/Turner1;component/Images/Roberto{0}/{1}b.png", App.PaintingChoice, (i + 1).ToString()), UriKind.Relative);
                Painting painting = new Painting(frontImageUri, backImageUri, i, this);
                if (App.PaintingChoice == 3)
                {
                    Uri frontSoundSource = new Uri(string.Format("/Turner1;component/Sounds/Roberto{0}/{1}a.mp3", App.PaintingChoice, (i + 1).ToString()), UriKind.Relative);
                    Uri backSoundSource = new Uri(string.Format("/Turner1;component/Sounds/Roberto{0}/{1}b.mp3", App.PaintingChoice, (i + 1).ToString()), UriKind.Relative);
                    
                    painting.AddFrontSound(frontSoundSource);
                    painting.AddBackSound(backSoundSource);
                }
                _paintings.Add(painting);
            }
           
        }

        public PaintingGridEncoding GenerateEncoding()
        {
            PaintingGridEncoding gridEncoding = new PaintingGridEncoding(true);
            PaintingEncoding[] array = new PaintingEncoding[12];

            for (int i = 0; i < _paintings.Count; i++)
            {

                Painting painting = _paintings[i];
                int row = (int)painting.GetValue(Grid.RowProperty);
                int column = (int)painting.GetValue(Grid.ColumnProperty);
                int index = column + (row * MainPage.NUMBER_OF_COLUMNS);
                array[index] = painting.GenerateEncoding();
             
            }
            for (int i = 0; i < array.Length; i++)
            {
                PaintingEncoding encoding = array[i];
                gridEncoding.AddPaintingEncoding(encoding);
            }

            return gridEncoding;
        }
        

        private void PopulateGrid()
        {

            for (int row = 0; row < MainPage.NUMBER_OF_ROWS; row++)
            {
                for (int column = 0; column < MainPage.NUMBER_OF_COLUMNS; column++)
                {

                    Painting painting = _paintings[column + (row * MainPage.NUMBER_OF_COLUMNS)];
                    LayoutRoot.Children.Add(painting);
                    painting.SetValue(Grid.ColumnProperty, column);
                    painting.SetValue(Grid.RowProperty, row);
                }
            }
        }

        private void BackgroundMusic_MediaEnded(object sender, RoutedEventArgs e)
        {
            PlayBackgroundMusic();
        }

        private void BackgroundMusic_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            BackgroundMusic.Source = null;
        }
    }
}
