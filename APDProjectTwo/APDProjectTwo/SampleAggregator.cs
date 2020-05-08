using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using NAudio.Dsp;
using NAudio.Wave;
using System.Windows.Shapes;

namespace APDProjectTwo
{
    public class SampleAggregator : ISampleProvider
    {
        // volume
        public event EventHandler<MaxSampleEventArgs> MaximumCalculated;
        //private float maxValue;
        //private float minValue;
        public int NotificationCount { get; set; }
        //int count;

        // FFT
        public event EventHandler<FftEventArgs> FftCalculated;
        public bool PerformFFT { get; set; }
        private readonly Complex[] fftBuffer;
        private readonly FftEventArgs fftArgs;
        private int fftPos;
        private readonly int fftLength;
        private readonly int m;
        private readonly ISampleProvider source;

        private readonly int channels;

        private Func<int, int, double> windowFunction;

        public static Func<int, int, double>[] windowFunctions = {
            RectangularWindow,
            FastFourierTransform.HammingWindow,
            FastFourierTransform.HannWindow
        };

        private static double RectangularWindow(int n, int frameSize)
        {
            return 1.0;
        }

        public SampleAggregator(ISampleProvider source, int fftLength, Func<int, int, double> winFun)
        {
            channels = source.WaveFormat.Channels;
            if (!IsPowerOfTwo(fftLength))
            {
                throw new ArgumentException("FFT Length must be a power of two (" + fftLength + ")");
            }
            m = (int)Math.Log(fftLength, 2.0);
            this.fftLength = fftLength;
            fftBuffer = new Complex[fftLength];
            fftArgs = new FftEventArgs(fftBuffer);
            this.source = source;
            windowFunction = winFun;
        }

        static bool IsPowerOfTwo(int x)
        {
            return (x & (x - 1)) == 0;
        }

        //public void Reset()
        //{
        //    count = 0;
        //    maxValue = minValue = 0;
        //}

        private void Add(float value)
        {
            if (PerformFFT && FftCalculated != null)
            {

                fftBuffer[fftPos].X = (float)(value * windowFunction(fftPos, fftLength));
                fftBuffer[fftPos].Y = 0;
                fftPos++;
                //Debug.Print(fftPos.ToString());
                if (fftPos >= fftBuffer.Length)
                {
                    Debug.Print("Calcing");
                    fftPos = 0;
                    FastFourierTransform.FFT(true, m, fftBuffer);
                    FftCalculated(this, fftArgs);
                }
            }

            //maxValue = Math.Max(maxValue, value);
            //minValue = Math.Min(minValue, value);
            //count++;
            //if (count >= NotificationCount && NotificationCount > 0)
            //{
            //    MaximumCalculated?.Invoke(this, new MaxSampleEventArgs(minValue, maxValue));
            //    Reset();
            //}
        }

        public WaveFormat WaveFormat => source.WaveFormat;

        public int Read(float[] buffer, int offset, int count)
        {
            var samplesRead = source.Read(buffer, offset, count);

            for (int n = 0; n < samplesRead; n += channels)
            {
                Add(buffer[n + offset]);
            }
            Debug.Print("samplesRead: " + samplesRead);
            return samplesRead;
        }
    }

    public class MaxSampleEventArgs : EventArgs
    {
        [DebuggerStepThrough]
        public MaxSampleEventArgs(float minValue, float maxValue)
        {
            MaxSample = maxValue;
            MinSample = minValue;
        }
        public float MaxSample { get; private set; }
        public float MinSample { get; private set; }
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
