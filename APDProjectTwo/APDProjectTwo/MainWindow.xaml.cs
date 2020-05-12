using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Microsoft.Win32;
using NAudio;
using NAudio.Wave;
using NAudio.Dsp;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;

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
        private int spectroInd;
        private double[,] spectrogramPoints;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Wav files (*.wav)|*.wav";
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

                QueueFFT();
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

        private void QueueFFT()
        {
            if (samples == null)
            {
                return;
            }

            Debug.Print("queued");
            // Default to single frame
            int fftLength = viewModel.SamplesPerFrame;
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
            var spectrogramAggregator = new SampleAggregator(fftLength, winFun)
            {
                PerformFFT = true
            };
            spectrogramAggregator.FftCalculated += (s, a) => OnSpectroFftCalculated(a.Result);
            spectroInd = 0;
            int rows = samples.Length / channels / viewModel.SamplesPerFrame;
            int cols = fftLength / 2;
            Debug.Print("{0} x {1}", rows, cols);
            spectrogramPoints = new double[rows, cols];

            for (int n = 0; n < samples.Length; n += channels)
            {
                spectrogramAggregator.Add(samples[n]);
            }
        }

        private void OnFftCalculated(Complex[] result)
        {
            Debug.Print("Calculated FFT");
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
            // Update spectrogram plot
            Debug.Print("shiiet {0}", spectroInd);
            for (int i = 0; i < result.Length / 2; i++)
            {
                spectrogramPoints[spectroInd, i] = GetDb(result[i]);
            }
            spectroInd++;
            if(spectroInd == samples.Length / channels / viewModel.SamplesPerFrame)
            {
                viewModel.SpectrogramPoints = spectrogramPoints;
            }
        }

        private double GetDb(Complex c)
        {
            return 10 * Math.Log10(Math.Sqrt(c.X * c.X + c.Y * c.Y));
        }

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
            QueueFFT();
        }
    }
}
