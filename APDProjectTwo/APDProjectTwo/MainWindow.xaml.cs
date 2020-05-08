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

namespace APDProjectTwo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WaveStream fileStream;
        private int sampleRate;
        private int channels;
        private MainViewModel viewModel = new MainViewModel();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
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
                var inputStream = new AudioFileReader(fileName);
                fileStream = inputStream;
                sampleRate = inputStream.WaveFormat.SampleRate;
                channels = inputStream.WaveFormat.Channels;
                viewModel.OpenFileName = fileName;
                QueueFFT(inputStream);
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

        private void QueueFFT(AudioFileReader inputStream)
        {
            int fftLength = viewModel.SamplesPerFrame;
            if (lengthBox.SelectedIndex == 1)
            {
                // Whole signal (cut when too long)
                int len = (int)inputStream.Length / sizeof(float) / channels;
                int m = (int)Math.Log(len, 2.0);
                Debug.Print("len " + len);
                Debug.Print("pow " + m);
                fftLength = (1 << m);
                Debug.Print("fft " + fftLength);
            }

            int ind = windowCombo.SelectedIndex;
            var aggregator = new SampleAggregator(inputStream, fftLength, SampleAggregator.windowFunctions[ind]);
            //aggregator.NotificationCount = sampleRate / 100;
            aggregator.PerformFFT = true;
            aggregator.FftCalculated += (s, a) => OnFftCalculated(a.Result);
            aggregator.MaximumCalculated += (s, a) => OnMaxCalculated(a.MinSample, a.MaxSample);

            float[] buffer = new float[fftLength * channels];
            aggregator.Read(buffer, 0, fftLength * channels);
        }

        public void OnMaxCalculated(float min, float max)
        {
            // nothing to do
        }

        public void OnFftCalculated(Complex[] result)
        {
            Debug.Print("Calculated FFT");
            List<DataPoint> itemsSource = new List<DataPoint>();
            for(int i = 0; i <= result.Length / 2; i++)
            {
                double freq = i * sampleRate / result.Length;
                itemsSource.Add(new DataPoint(freq, GetDb(result[i])));
            }

            //spectrumAnalyser.Update(result);
            viewModel.FftPoints = itemsSource;
        }

        private double GetDb(Complex c)
        {
            return 20 * Math.Log10(Math.Sqrt(c.X * c.X + c.Y * c.Y));
        }
    }
}
