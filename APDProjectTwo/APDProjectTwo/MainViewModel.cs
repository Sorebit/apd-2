namespace APDProjectTwo
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using OxyPlot;
    using OxyPlot.Series;

    public class MainViewModel
    {
        public MainViewModel()
        {
            MyModel = new PlotModel();
            MyModel.Series.Add(new FunctionSeries(Math.Cos, 0, 10, 0.1, "cos(x)"));

            Title = "Example 2";
            Points = new List<DataPoint>
            {
                new DataPoint(0, 4),
                new DataPoint(10, 13),
                new DataPoint(20, 15),
                new DataPoint(30, 16),
                new DataPoint(40, 12),
                new DataPoint(50, 12)
            };

        }

        public float Overlap;
        public PlotModel MyModel { get; private set; }
        public string Title { get; private set; }
        public IList<DataPoint> Points { get; private set; }
    }
}

