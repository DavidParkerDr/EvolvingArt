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
    public class Pixel
    {
        public int A
        {
            get;
            set;
        }
        public int R
        {
            get;
            set;
        }
        public int G
        {
            get;
            set;
        }
        public int B
        {
            get;
            set;
        }

        public Pixel(int a, int r, int g, int b)
        {
            A = a;
            R = r;
            G = g;
            B = b;
        }

        public Pixel(int pixelAsInt)
        {
            A = ((pixelAsInt >> 0x18) & 0xff);
            R = ((pixelAsInt >> 0x10) & 0xff);
            G = ((pixelAsInt >> 8) & 0xff);
            B = (pixelAsInt & 0xff);
        }

        public double Distance(Pixel other)
        {
            Pixel difference = new Pixel(A - other.A, R - other.R, G - other.G, B - other.B);
            return Math.Sqrt(Math.Pow(difference.A, 2) + Math.Pow(difference.R, 2) + Math.Pow(difference.G, 2) + Math.Pow(difference.B, 2));
        }

    }
}
