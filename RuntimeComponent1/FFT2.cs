using System;
using System.Collections.Generic;
using System.Numerics;

namespace AudioProcessing
{
    public sealed class FFT2
    {

        public IList<float> run(IList<float> points)
        {

            List<Complex> pointsw = new List<Complex>();

            for (int i = 0; i < points.Count; i++)
            {
                pointsw.Add(points[i]);
            }

            fft(pointsw);


            for (int i = 0; i < points.Count; i++)
            {
                points[i] = (float)pointsw[i].Magnitude;
            }

            return points;
        }

        private void fft(List<Complex> x)
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
            fft(even);
            fft(odd);

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