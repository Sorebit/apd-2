using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Win32;
using NAudio.Wave;
using OxyPlot;
using System.Numerics;
using MathNet.Numerics.IntegralTransforms;

namespace APDProjectTwo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WaveStream fileStream;
        private AudioFileReader inputStream;
        private int sampleRate;
        private int channels;
        private MainViewModel viewModel = new MainViewModel();
        private float[] samples;
        private List<double[]> _spectroPoints;
        private double[,] spectroData;
        private int fftLength;
        private List<Complex[]> cepstrumData;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Wav files (*.wav)|*.wav"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                OpenFile(openFileDialog.FileName);
            }
        }

        private void OpenFile(string fileName)
        {
            try
            {
                inputStream = new AudioFileReader(fileName);
                fileStream = inputStream;
                sampleRate = inputStream.WaveFormat.SampleRate;
                channels = inputStream.WaveFormat.Channels;
                viewModel.OpenFileName = fileName;

                samples = new float[inputStream.Length / sizeof(float)];
                inputStream.Read(samples, 0, samples.Length);

                AddWaveform();

                Process();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Problem opening file");
                CloseFile();
            }
        }

        private void CloseFile()
        {
            fileStream?.Dispose();
            fileStream = null;
        }

        private void Process()
        {
            if (samples == null)
            {
                return;
            }

            // Default to single frame
            // 0 - single frame
            // 1 - whole signal
            fftLength = viewModel.SamplesPerFrame;
            int offset = viewModel.SampleStart;
            if (viewModel.SelectedLength == 1)
            {
                // Whole signal (cut when too long)
                offset = 0;
                int len = samples.Length / channels;
                int m = (int)Math.Log(len, 2.0);
                fftLength = (1 << m);
            }

            if (viewModel.SampleStart + fftLength * channels > samples.Length)
            {
                MessageBox.Show("Sample start too high", "Error");
                return;
            }

            // FFT
            Func <int, int, double> winFun = SampleAggregator.windowFunctions[windowCombo.SelectedIndex];
            var aggregator = new SampleAggregator(fftLength, winFun)
            {
                PerformFFT = true
            };
            aggregator.FftCalculated += (s, a) => OnFftCalculated(a.Result);

            // Add samples from single channel to FFT aggregator
            for (int n = offset; n < fftLength * channels + offset; n += channels)
            {
                aggregator.Add(samples[n]);
            }

            // Add samples for spectrogram
            var spectrogramAggregator = new SampleAggregator(viewModel.SamplesPerFrame, winFun)
            {
                PerformFFT = true
            };
            spectrogramAggregator.FftCalculated += (s, a) => OnSpectroFftCalculated(a.Result);

            int jumpBack = (int)((float)fftLength * viewModel.Overlap);
            _spectroPoints = new List<double[]>();

            int sp = 0;
            int currentN = 0;
            // Iterate over samples, jumping back if overlap is required
            while (sp < samples.Length)
            {
                spectrogramAggregator.Add(samples[sp]);
                sp += channels;
                currentN++;
                if(jumpBack > 0 && currentN == viewModel.SamplesPerFrame)
                {
                    currentN = 0;
                    sp -= jumpBack * channels;
                }
            }

            // Update plot
            int snapshots = _spectroPoints[0].Length;
            spectroData = new double[_spectroPoints.Count, snapshots];
            for (int i = 0; i < _spectroPoints.Count; i++)
            {
                for (int j = 0; j < snapshots; j++)
                {
                    spectroData[i, j] = _spectroPoints[i][j];
                }
            }
            viewModel.SpectrogramData = spectroData;

            // Cepstrum
            cepstrumData = new List<Complex[]>();
            List<DataPoint> cepstrumPoints = new List<DataPoint>();
            for (int i = 0; i < _spectroPoints.Count; i++)
            {

                cepstrumData.Add(new Complex[_spectroPoints[i].Length]);
                for (int j = 0; j < cepstrumData[i].Length; j++)
                {
                    // Convert back to non-db
                    double re = Math.Pow(10, (_spectroPoints[i][j] / 10.0));
                    cepstrumData[i][j] = new Complex(Math.Log(re), 0);
                }
                
                try
                {
                    Fourier.Inverse(cepstrumData[i], FourierOptions.Matlab);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Error: " + ex.Message);
                }

                double tMax = cepstrumData[i][50].Real;
                for (int j = 50; j < cepstrumData[i].Length / 2; j++)
                {
                    tMax = Math.Max(tMax, cepstrumData[i][j].Real);
                }
                cepstrumPoints.Add(new DataPoint(i, 1 / tMax));
            }
            viewModel.CepstrumPoints = cepstrumPoints;
        }

        private void OnFftCalculated(Complex[] result)
        {
            List<DataPoint> points = new List<DataPoint>();
            for(int i = 0; i < result.Length / 2; i++)
            {
                double freq = i * sampleRate / result.Length;
                points.Add(new DataPoint(freq, GetDb(result[i])));
            }

            viewModel.FftPoints = points;
        }

        private void OnSpectroFftCalculated(Complex[] result)
        {
            // Update spectrogram data
            double[] tmp = new double[result.Length / 2];
            for (int i = 0; i < result.Length / 2; i++)
            {
                tmp[i] = GetDb(result[i]);
            }
            _spectroPoints.Add(tmp);
        }

        private double GetDb(Complex c)
        {
            return 10.0 * Math.Log10(Math.Sqrt(c.Real * c.Real + c.Imaginary * c.Imaginary));
        }

        // Loads waveform into view
        public void AddWaveform()
        {
            List<DataPoint> points = new List<DataPoint>();
            for (int n = 0; n < samples.Length; n += channels)
            {
                points.Add(new DataPoint(n, samples[n]));
            }
            viewModel.WaveformPoints = points;
        }

        private void Redraw_Click(object sender, RoutedEventArgs e)
        {
            Process();
        }
    }
}
