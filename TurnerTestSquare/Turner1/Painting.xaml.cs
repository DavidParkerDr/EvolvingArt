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
using System.Windows.Resources;

namespace Turner1
{
    public partial class Painting : UserControl
    {
        PaintingGrid _parent;
        bool _visibleSideIsFront = true;
        System.Random RandNum = new System.Random();

        private Storyboard _animateRotate0To90 = new Storyboard();
        private Storyboard _animateRotate90To180 = new Storyboard();
        private Storyboard _animateRotate180To270 = new Storyboard();
        private Storyboard _animateRotate270To360 = new Storyboard();
        private Storyboard _animateFlipHorizontal = new Storyboard();
        private Storyboard _animateFlipVertical = new Storyboard();
        private TimeSpan _timeSpanLastFrameHorizontal = new TimeSpan();
        private TimeSpan _timeSpanLastFrameVertical = new TimeSpan();
        //private ScaleTransform _scaleTransform = new ScaleTransform();
        //private RotateTransform _rotateTransform = new RotateTransform();

        public int PaintingIndex
        {
            get;
            set;
        }

        public bool FrontVisible
        {

            get
            {
                return _visibleSideIsFront;
            }
        }

        private int _rotation = 0;
        public int Rotation
        {
            get
            {
                return _rotation;
            }
            set
            {
                _rotation = value;
            }
        }
        

        public Painting(Uri frontUri, Uri backUri, int id, PaintingGrid parent)
        {
            InitializeComponent();
            _parent = parent;
            PaintingImageFront.Source = new BitmapImage(frontUri);
            PaintingImageBack.Source = new BitmapImage(backUri);
            Buttons.Visibility = Visibility.Collapsed;
            MoveButton.Visibility = Visibility.Collapsed;
            PaintingIndex = id;


            RotateTransform rotate = (RotateTransform)Sides.FindName("PaintingRotate");
            DefineAnimateSpin(_animateRotate0To90, rotate, 0, 90);
            DefineAnimateSpin(_animateRotate90To180, rotate, 90, 180);
            DefineAnimateSpin(_animateRotate180To270, rotate, 180, 270);
            DefineAnimateSpin(_animateRotate270To360, rotate, 270, 360);

            ScaleTransform scale = (ScaleTransform)Sides.FindName("PaintingScale");
            DefineAnimateHorizontalFlip(_animateFlipHorizontal, scale, out _timeSpanLastFrameHorizontal);
            DefineAnimateVerticalFlip(_animateFlipVertical, scale, out _timeSpanLastFrameVertical);
            PaintingImageBack.Opacity = 0;
            
        }

        public Painting(WriteableBitmap frontBitmap, WriteableBitmap backBitmap, int id, PaintingGrid parent)
        {
            InitializeComponent();
            _parent = parent;
            PaintingImageFront.Source = frontBitmap;
            PaintingImageBack.Source = backBitmap;


            Sides.Width = frontBitmap.PixelWidth;
            Sides.Height = frontBitmap.PixelHeight;
            PaintingImageFront.Width = frontBitmap.PixelWidth;
            PaintingImageFront.Height = frontBitmap.PixelHeight;
            PaintingImageBack.Width = backBitmap.PixelWidth;
            PaintingImageBack.Height = backBitmap.PixelHeight;
            Buttons.Visibility = Visibility.Collapsed;
            MoveButton.Visibility = Visibility.Collapsed;
            PaintingIndex = id;

            double buttonScale = frontBitmap.PixelWidth / 175.0;
            FlipHorizontalButton.Width = 55 * buttonScale;
            FlipHorizontalButton.Height = 70 * buttonScale;
            Rotate180Button.Width = 55 * buttonScale;
            Rotate180Button.Height = 70 * buttonScale;
            FlipVerticalButton.Width = 55 * buttonScale;
            FlipVerticalButton.Height = 70 * buttonScale;
            MoveButton.Width = 41 * buttonScale;
            MoveButton.Height = 41 * buttonScale;
            

            //FLipHorizontalButton.FlipHorizontalImage.Width = 55 * buttonScale;
            //FlipHorizontalImage.Height = 70 * buttonScale;
            //Rotate180Image.Width = 55 * buttonScale;
            //Rotate180Image.Height = 70 * buttonScale;
            //FlipVerticalImage.Width = 55 * buttonScale;
            //FlipVerticalImage.Height = 70 * buttonScale;
            //MoveImage.Width = 41 * buttonScale;
            //MoveImage.Height = 41 * buttonScale;

            RotateTransform rotate = (RotateTransform)Sides.FindName("PaintingRotate");
            DefineAnimateSpin(_animateRotate0To90, rotate, 0, 90);
            DefineAnimateSpin(_animateRotate90To180, rotate, 90, 180);
            DefineAnimateSpin(_animateRotate180To270, rotate, 180, 270);
            DefineAnimateSpin(_animateRotate270To360, rotate, 270, 360);

            ScaleTransform scale = (ScaleTransform)Sides.FindName("PaintingScale");
            DefineAnimateHorizontalFlip(_animateFlipHorizontal, scale, out _timeSpanLastFrameHorizontal);
            DefineAnimateVerticalFlip(_animateFlipVertical, scale, out _timeSpanLastFrameVertical);
            PaintingImageBack.Opacity = 0;

        }

        public void AddFrontSound(Uri fileName)
        {
            FrontSound.Stop();
            StreamResourceInfo sri = Application.GetResourceStream(fileName);
            if (sri != null)
            {
                FrontSound.SetSource(sri.Stream);
            }
        }
        public void AddBackSound(Uri fileName)
        {
            BackSound.Stop();
            StreamResourceInfo sri = Application.GetResourceStream(fileName);
            if (sri != null)
            {
                BackSound.SetSource(sri.Stream);
            }
        }

        public PaintingEncoding GenerateEncoding()
        {
            PaintingEncoding encoding = new PaintingEncoding(PaintingIndex, Rotation, FrontVisible);
            return encoding;
        }

        private void DefineAnimateSpin(Storyboard sb, RotateTransform rotate, int angleFrom, int angleTo)
        {
            DoubleAnimation doubleAnimation = new DoubleAnimation();
            doubleAnimation.Duration = new TimeSpan(0, 0, 0, 0, 250);
            doubleAnimation.From = angleFrom;
            doubleAnimation.To = angleTo;
            Storyboard.SetTarget(doubleAnimation, rotate);
            Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath("(RotateTransform.Angle)"));
            sb.Children.Add(doubleAnimation);

        }

        public void performHorizontalFlip()
        {
            if (_visibleSideIsFront)
            {
                _animateFlipHorizontal.Stop();
                _animateFlipHorizontal.AutoReverse = false;
                _animateFlipHorizontal.Begin();
                _visibleSideIsFront = false;
                PaintingImageFront.Opacity = 0;
                PaintingImageBack.Opacity = 1;
            }
            else
            {
                _animateFlipHorizontal.Stop();
                _animateFlipHorizontal.AutoReverse = true;
                _animateFlipHorizontal.Seek(_timeSpanLastFrameHorizontal);
                _animateFlipHorizontal.Resume();
                _visibleSideIsFront = true;
                PaintingImageFront.Opacity = 1;
                PaintingImageBack.Opacity = 0;
            }
            PlaySound();
        }

        public void Randomise()
        {

            
            if (MainPage.FlipCoin())
            {
                performSpin();
            }

            if (MainPage.FlipCoin())
            {
                performHorizontalFlip();
            }
        }

        public void performVerticalFlip()
        {
            
            if (_visibleSideIsFront)
            {
                _animateFlipVertical.Pause();
                _animateFlipVertical.AutoReverse = false;
                _animateFlipVertical.Begin();
                _visibleSideIsFront = false;
                PaintingImageFront.Opacity = 0;
                PaintingImageBack.Opacity = 1;
            }
            else
            {
                _animateFlipVertical.Pause();
                _animateFlipVertical.AutoReverse = true;
                _animateFlipVertical.Seek(_timeSpanLastFrameVertical);
                _animateFlipVertical.Resume();
                _visibleSideIsFront = true;
                PaintingImageFront.Opacity = 1;
                PaintingImageBack.Opacity = 0;
                
            }

            PlaySound();

            RotateTransform rotate = (RotateTransform)Sides.FindName("PaintingRotate");
            
            if (rotate.Angle == 270)
            {
                rotate.Angle = 90;
                Rotation = 90;
            }
            else if (rotate.Angle == 180)
            {
                rotate.Angle = 0;
                Rotation = 0;
            }
            else
            {
                rotate.Angle += 180;
                Rotation += 180;
            }
        }

        public void StopSounds()
        {
            BackSound.Stop();
            FrontSound.Stop();
        }

        public void PlayBackSound()
        {
            if (BackSound.Source != null)
            {
                BackSound.Stop();
                BackSound.Play();
            }
        }
        public void PlayFrontSound()
        {
            if (FrontSound.Source != null)
            {
                FrontSound.Stop();
                FrontSound.Play();
            }
        }

        public void performSpin()
        {
            //this.Media = _media;
            RotateTransform rotate = (RotateTransform)Sides.FindName("PaintingRotate");
            if (Rotation == 0)
            {
                _animateRotate0To90.Pause();
                _animateRotate0To90.AutoReverse = false;
                _animateRotate0To90.Begin();
                Rotation = 90;

            }
            else if (Rotation == 90)
            {
                _animateRotate90To180.Pause();
                _animateRotate90To180.AutoReverse = false;
                _animateRotate90To180.Begin();
                Rotation = 180;

            }
            else if (Rotation == 180)
            {
                _animateRotate180To270.Pause();
                _animateRotate180To270.AutoReverse = false;
                _animateRotate180To270.Begin();
                Rotation = 270;

            }
            else if (Rotation == 270)
            {
                _animateRotate270To360.Pause();
                _animateRotate270To360.AutoReverse = false;
                _animateRotate270To360.Begin();
                Rotation = 0;
            }

            PlaySound();

        }

        public void PlaySound()
        {
            if (_visibleSideIsFront)
            {
                PlayFrontSound();
            }
            else
            {
                PlayBackSound();
            }
        }

        private void DefineAnimateVerticalFlip(Storyboard sb, ScaleTransform scale, out TimeSpan tsLastFrame)
        {
            double speed = 4;
            double flipRotation = 0;
            double flipped = 2;
            tsLastFrame = new TimeSpan();
            TimeSpan tsSideFlipped = new TimeSpan();

            int frames = 1;
            DoubleAnimationUsingKeyFrames daX = new DoubleAnimationUsingKeyFrames();
            tsLastFrame = new TimeSpan();
            while (flipRotation != flipped * 180)
            {
                flipRotation += speed;
                double flipRadian = flipRotation * (Math.PI / 180);
                double size = Math.Cos(flipRadian);
                double scalar = (1 / (1 / size));

                DiscreteDoubleKeyFrame ddkfX = new DiscreteDoubleKeyFrame();
                ddkfX.Value = (size * scalar);

                tsLastFrame = TimeSpan.FromMilliseconds(frames * 7);

                //the first time we flip to negative capture the tsLastFrame so we know when we will need to
                //visualize the flip side
                flipped = (size < 0) ? +1 : +0;
                if (flipped == 1 && tsSideFlipped.Ticks == 0)
                {
                    tsSideFlipped = tsLastFrame;
                }

                ddkfX.KeyTime = KeyTime.FromTimeSpan(tsLastFrame);

                //add the DiscreteDoubleKeyFrame to the DoubleAnimationUsingKeyFrames
                daX.KeyFrames.Add(ddkfX);

                flipRotation %= 360;
                frames++;
            }

            Storyboard.SetTarget(daX, scale);
            Storyboard.SetTargetProperty(daX, new PropertyPath("(ScaleY)"));
            sb.Children.Add(daX);

            VisualizeSide(sb, tsSideFlipped, 0, TimeSpan.FromMilliseconds((tsSideFlipped.Milliseconds + 100)), PaintingImageBack.Opacity, PaintingImageBack);
            VisualizeSide(sb, TimeSpan.FromMilliseconds((tsSideFlipped.Milliseconds - 100)), PaintingImageFront.Opacity, tsSideFlipped, 0, PaintingImageFront);
            
        }

        private void DefineAnimateHorizontalFlip(Storyboard sb, ScaleTransform scale, out TimeSpan tsLastFrame)
        {
            double speed = 4;
            double flipRotation = 0;
            double flipped = 2;
            tsLastFrame = new TimeSpan();
            TimeSpan tsSideFlipped = new TimeSpan();

            int frames = 1;
            DoubleAnimationUsingKeyFrames daX = new DoubleAnimationUsingKeyFrames();
            tsLastFrame = new TimeSpan();
            while (flipRotation != flipped * 180)
            {
                flipRotation += speed;
                double flipRadian = flipRotation * (Math.PI / 180);
                double size = Math.Cos(flipRadian);
                double scalar = (1 / (1 / size));

                DiscreteDoubleKeyFrame ddkfX = new DiscreteDoubleKeyFrame();
                ddkfX.Value = (size * scalar);

                tsLastFrame = TimeSpan.FromMilliseconds(frames * 7);

                //the first time we flip to negative capture the tsLastFrame so we know when we will need to
                //visualize the flip side
                flipped = (size < 0) ? +1 : +0;
                if (flipped == 1 && tsSideFlipped.Ticks == 0)
                {
                    tsSideFlipped = tsLastFrame;
                }

                ddkfX.KeyTime = KeyTime.FromTimeSpan(tsLastFrame);

                //add the DiscreteDoubleKeyFrame to the DoubleAnimationUsingKeyFrames
                daX.KeyFrames.Add(ddkfX);

                flipRotation %= 360;
                frames++;
            }

            Storyboard.SetTarget(daX, scale);
            Storyboard.SetTargetProperty(daX, new PropertyPath("(ScaleX)"));
            sb.Children.Add(daX);

            VisualizeSide(sb, tsSideFlipped, 0, TimeSpan.FromMilliseconds((tsSideFlipped.Milliseconds + 100)), PaintingImageBack.Opacity, PaintingImageBack);
            VisualizeSide(sb, TimeSpan.FromMilliseconds((tsSideFlipped.Milliseconds - 100)), PaintingImageFront.Opacity, tsSideFlipped, 0, PaintingImageFront);
           
        }

        private static KeySpline DefineKeySpline(double cp1X, double cp1Y, double cp2X, double cp2Y)
        {
            KeySpline ksStart = new KeySpline();
            ksStart.ControlPoint1 = new Point(cp1X, cp1Y);
            ksStart.ControlPoint2 = new Point(cp2X, cp2Y);
            return ksStart;
        }
        private void VisualizeSide(Storyboard sb, TimeSpan tsStart, double opacityStart, TimeSpan tsEnd, double opacityEnd, UIElement side)
        {
            DoubleAnimationUsingKeyFrames daOpacity = new DoubleAnimationUsingKeyFrames();
            SplineDoubleKeyFrame sdkfStart = new SplineDoubleKeyFrame();
            sdkfStart.Value = opacityStart;
            sdkfStart.KeyTime = tsStart;
            sdkfStart.KeySpline = DefineKeySpline(0, 0, 1, 1);
            daOpacity.KeyFrames.Add(sdkfStart);

            SplineDoubleKeyFrame sdkfEnd = new SplineDoubleKeyFrame();
            sdkfEnd.Value = opacityEnd;
            sdkfEnd.KeyTime = tsEnd;
            sdkfEnd.KeySpline = DefineKeySpline(0, 0, 1, 1);
            daOpacity.KeyFrames.Add(sdkfEnd);

            Storyboard.SetTarget(daOpacity, side);
            Storyboard.SetTargetProperty(daOpacity, new PropertyPath("(UIElement.Opacity)"));

            sb.Children.Add(daOpacity);
        }

        public void Configure(PaintingEncoding configuration, bool animate = false)
        {
            if (animate)
            {
                if (configuration.FrontVisible != _visibleSideIsFront)
                {
                    performHorizontalFlip();
                }

                RotateTransform rotate = (RotateTransform)Sides.FindName("PaintingRotate");
                rotate.Angle = 0;
                if (configuration.Rotation != 0)
                {
                    rotate.Angle = configuration.Rotation - 90;
                    performSpin();
                }
                
                
            }
            else
            {
                _visibleSideIsFront = configuration.FrontVisible;
                if (_visibleSideIsFront)
                {
                    PaintingImageFront.Opacity = 1;
                    PaintingImageBack.Opacity = 0;
                }
                else
                {
                    PaintingImageFront.Opacity = 0;
                    PaintingImageBack.Opacity = 1;
                }

                PlaySound();

               // RotateTransform rotate = (RotateTransform)Sides.FindName("PaintingRotate");
              //  rotate.Angle = configuration.Rotation;
                
            }
        }

        
        public void HideUI()
        {
            Buttons.Visibility = Visibility.Collapsed;
            MoveButton.Visibility = Visibility.Collapsed;
            MoveButton.VerticalAlignment = VerticalAlignment.Top;
            MoveButton.HorizontalAlignment = HorizontalAlignment.Right;
            Sides.Opacity = 1;
        }

        public void ShowUI()
        {
            if (_parent.ManualMode)
            {
                MoveButton.Visibility = Visibility.Visible;
                Sides.Opacity = 0.5;

                if (_parent.MoveMode)
                {
                    Buttons.Visibility = Visibility.Collapsed;
                    MoveButton.VerticalAlignment = VerticalAlignment.Center;
                    MoveButton.HorizontalAlignment = HorizontalAlignment.Center;
                }
                else
                {
                    Buttons.Visibility = Visibility.Visible;
                    MoveButton.VerticalAlignment = VerticalAlignment.Top;
                    MoveButton.HorizontalAlignment = HorizontalAlignment.Right;
                }
            }
        }



        private void LayoutRoot_MouseEnter(object sender, MouseEventArgs e)
        {
            if (_parent.ManualMode)
            {
                Sides.Opacity = 0.5;
                if (!_parent.MoveMode)
                {
                    Buttons.Visibility = Visibility.Visible;
                    MoveButton.Visibility = Visibility.Visible;

                }
            }
        }

        private void LayoutRoot_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!_parent.MoveMode)
            {
                Sides.Opacity = 1;
                Buttons.Visibility = Visibility.Collapsed;
                MoveButton.Visibility = Visibility.Collapsed;
               
            }
            else if (_parent.MoveFrom != this)
            {
                Sides.Opacity = 1;
            }
        }

        private void FlipHorizontalButton_Click(object sender, RoutedEventArgs e)
        {
            performHorizontalFlip();
            
        }

        private void Rotate180Button_Click(object sender, RoutedEventArgs e)
        {
               
            performSpin();

            
        }
        private void MoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_parent.MoveMode)
            {
                _parent.MoveMode = true;
                _parent.MoveFrom = this;
                Buttons.Visibility = Visibility.Collapsed;
                MoveButton.VerticalAlignment = VerticalAlignment.Center;
                MoveButton.HorizontalAlignment = HorizontalAlignment.Center;
            }
            else
            {
                _parent.MoveMode = false;
                _parent.MoveFrom = null;
                Buttons.Visibility = Visibility.Visible;
                MoveButton.VerticalAlignment = VerticalAlignment.Top;
                MoveButton.HorizontalAlignment = HorizontalAlignment.Right;
            }
        }
        public void CancelMoveMode()
        {
            Buttons.Visibility = Visibility.Visible;
            MoveButton.VerticalAlignment = VerticalAlignment.Top;
            MoveButton.HorizontalAlignment = HorizontalAlignment.Right;
        }

        private void MoveButton_MouseEnter(object sender, RoutedEventArgs e)
        {
            ScaleTransform scale = (ScaleTransform)((Image)sender).FindName("MoveScale");
            scale.CenterX = ((Image)sender).Width / 2;
            scale.CenterY = ((Image)sender).Height / 2;
            scale.ScaleX = 1.05;
            scale.ScaleY = 1.05;
        }
        private void MoveButton_MouseLeave(object sender, RoutedEventArgs e)
        {
            ScaleTransform scale = (ScaleTransform)((Image)sender).FindName("MoveScale");
            scale.CenterX = ((Image)sender).Width / 2;
            scale.CenterY = ((Image)sender).Height / 2;
            scale.ScaleX = 1;
            scale.ScaleY = 1;
        }

        private void FlipVerticalButton_Click(object sender, RoutedEventArgs e)
        {
            performVerticalFlip();
        }

        private void FlipHorizontalButton_MouseEnter(object sender, MouseEventArgs e)
        {
            ScaleTransform scale = (ScaleTransform)((Image)sender).FindName("FlipHorizontalScale");
            scale.CenterX = ((Image)sender).Width / 2;
            scale.CenterY = ((Image)sender).Height / 2;
            scale.ScaleX = 1.05;
            scale.ScaleY = 1.05;
        }

        private void FlipHorizontalButton_MouseLeave(object sender, MouseEventArgs e)
        {
            ScaleTransform scale = (ScaleTransform)((Image)sender).FindName("FlipHorizontalScale");
            scale.CenterX = ((Image)sender).Width / 2;
            scale.CenterY = ((Image)sender).Height / 2;
            scale.ScaleX = 1;
            scale.ScaleY = 1;
            
        }

        


        private void Rotate180Button_MouseEnter(object sender, MouseEventArgs e)
        {
            ScaleTransform scale = (ScaleTransform)((Image)sender).FindName("Rotate180Scale");
            scale.CenterX = ((Image)sender).Width / 2;
            scale.CenterY = ((Image)sender).Height / 2;
            scale.ScaleX = 1.05;
            scale.ScaleY = 1.05;
        }

        private void Rotate180Button_MouseLeave(object sender, MouseEventArgs e)
        {
            ScaleTransform scale = (ScaleTransform)((Image)sender).FindName("Rotate180Scale");
            scale.CenterX = ((Image)sender).Width / 2;
            scale.CenterY = ((Image)sender).Height / 2;
            scale.ScaleX = 1;
            scale.ScaleY = 1;
        }

        private void FlipVerticalButton_MouseEnter(object sender, MouseEventArgs e)
        {
            ScaleTransform scale = (ScaleTransform)((Image)sender).FindName("FlipVerticalScale");
            scale.CenterX = ((Image)sender).Width / 2;
            scale.CenterY = ((Image)sender).Height / 2;
            scale.ScaleX = 1.05;
            scale.ScaleY = 1.05;
        }

        private void FlipVerticalButton_MouseLeave(object sender, MouseEventArgs e)
        {
            ScaleTransform scale = (ScaleTransform)((Image)sender).FindName("FlipVerticalScale");
            scale.CenterX = ((Image)sender).Width / 2;
            scale.CenterY = ((Image)sender).Height / 2;
            scale.ScaleX = 1;
            scale.ScaleY = 1;
        }

        private void LayoutRoot_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_parent.MoveMode)
            {
                _parent.MoveTo = this;
                _parent.SwitchPaintings();
            }
        }

        private void FrontSound_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            FrontSound.Source = null;
        }

        private void BackSound_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            BackSound.Source = null;
        }

        private void FrontSound_MediaEnded(object sender, RoutedEventArgs e)
        {
            FrontSound.Play();
        }

        private void BackSound_MediaEnded(object sender, RoutedEventArgs e)
        {
            BackSound.Play();
        }
    }
}
