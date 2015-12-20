using System;
using System.Collections.Generic;
using System.Numerics;

namespace AudioProcessing
{
    public sealed class FFT2
    {

        public IList<float> Run(IList<float> points)
        {

            List<Complex> pointsw = new List<Complex>();

            for (int i = 0; i < points.Count; i++)
            {
                pointsw.Add(points[i]);
            }

            FFT(pointsw);

            // Get the real signal, from only the first half of the complex points
            for (int i = 0; i < points.Count / 2; i++)
            {
                points[i] = (float)pointsw[i].Magnitude;
            }

            return points;
        }

        private void FFT(List<Complex> x)
        {
            int N = x.Count;
            if (N <= 1) return;

            // divide
            List<Complex> even = new List<Complex>();
            List<Complex> odd = new List<Complex>();

            for (int i = 0; i < N / 2; i++)
            {
                even.Add(x[2 * i]);
                odd.Add(x[2 * i + 1]);
            }

            // conquer
            FFT(even);
            FFT(odd);

            // combine
            for (int k = 0; k < N / 2; ++k)
            {
                Complex t = Complex.FromPolarCoordinates(1.0, -2 * Math.PI * k / N) * odd[k];
                x[k] = even[k] + t;
                x[k + N / 2] = even[k] - t;
            }
        }
    }
}