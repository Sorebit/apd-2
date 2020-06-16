using System;
using System.Diagnostics;
using System.Numerics;
using MathNet.Numerics.IntegralTransforms;

namespace APDProjectTwo
{
    public class SampleAggregator
    {
        // FFT
        public event EventHandler<FftEventArgs> FftCalculated;
        private readonly Complex[] fftBuffer;
        private readonly FftEventArgs fftArgs;
        private int fftPos;
        private readonly int fftLength;

        private readonly Func<int, int, double> windowFunction;

        public static Func<int, int, double>[] windowFunctions = {
            RectangularWindow,
            NAudio.Dsp.FastFourierTransform.HammingWindow,
            NAudio.Dsp.FastFourierTransform.HannWindow
        };

        private static double RectangularWindow(int n, int frameSize)
        {
            return 1.0;
        }

        public SampleAggregator(int fftLength, Func<int, int, double> winFun)
        {
            if (!IsPowerOfTwo(fftLength))
            {
                throw new ArgumentException("FFT Length must be a power of two (" + fftLength + ")");
            }
            this.fftLength = fftLength;
            fftBuffer = new Complex[fftLength];
            fftArgs = new FftEventArgs(fftBuffer);
            windowFunction = winFun;
        }

        static bool IsPowerOfTwo(int x)
        {
            return (x & (x - 1)) == 0;
        }

        public void Add(float value)
        {
            if (FftCalculated != null)
            {
                fftBuffer[fftPos] = new Complex((float)(value * windowFunction(fftPos, fftLength)), 0);
                fftPos++;
                if (fftPos >= fftBuffer.Length)
                {
                    fftPos = 0;
                   
                    try
                    {
                        Fourier.Forward(fftBuffer, FourierOptions.NoScaling);
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show("Error: " + ex.Message);
                    }
                    FftCalculated(this, fftArgs);
                }
            }
        }
    }

    public class FftEventArgs : EventArgs
    {
        [DebuggerStepThrough]
        public FftEventArgs(Complex[] result)
        {
            Result = result;
        }
        public Complex[] Result { get; private set; }
    }
}
