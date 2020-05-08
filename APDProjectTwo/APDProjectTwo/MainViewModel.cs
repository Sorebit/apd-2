using System.Collections.Generic;
using System.ComponentModel;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace APDProjectTwo
{
    public class MainViewModel : ViewModelBase
    {
        private string _openFileName;
        public string OpenFileName {
            get => _openFileName;
            set => SetProperty(ref _openFileName, value);
        }

        private int _samplesPerFrame;
        public int SamplesPerFrame
        {
            get => _samplesPerFrame;
            set => SetProperty(ref _samplesPerFrame, value);
        }

        private float _overlap;
        public float Overlap
        {
            get => _overlap;
            set => SetProperty(ref _overlap, value);
        }

        private IList<DataPoint> _fftPoints;
        public IList<DataPoint> FftPoints
        {
            get => _fftPoints;
            set => SetProperty(ref _fftPoints, value);
        }

        public MainViewModel()
        {
            SamplesPerFrame = 1024;
        }
    }
}

