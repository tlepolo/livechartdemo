using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.ConditionalDraw;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel.Events;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.VisualElements;
using MathNet.Numerics.Statistics;
using Newtonsoft.Json.Linq;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace livechartdemo
{
    public class ViewModel
    {
        private bool _isDown = false;
        private ObservableCollection<SiteData> _values = new ObservableCollection<SiteData>();
        private Random random = new Random();
        public ISeries[] Series { get; set; }
        public Axis[] ScrollableAxes { get; set; }
        public Axis[] ScrollableYAxes { get; set; }
        public ISeries[] ScrollbarSeries { get; set; }
        public Axis[] InvisibleX { get; set; }
        public Axis[] InvisibleY { get; set; }
        public LiveChartsCore.Measure.Margin Margin { get; set; }
        public RectangularSection[] Thumbs { get; set; }

        public ViewModel()
        {

            _values = MockData();
            Series = new ISeries[]
            {
                 new LineSeries<SiteData>
                {
                Values = _values,
                GeometryStroke = null,
                GeometryFill = new SolidColorPaint(SKColors.Blue),
                GeometrySize=5,
                Fill=null,
                LineSmoothness=0,
                Mapping = (s, p) =>
                 {
                    p.Coordinate=new LiveChartsCore.Kernel.Coordinate(p.Index,s.SiteValue);
                 },
               DataPadding=new LiveChartsCore.Drawing.LvcPoint(0,1),

                }.OnPointMeasured(p=>{
                    SiteData site = p.Model;
                    if (site.Sigma>3)
    {
                        p.Visual.Fill=new SolidColorPaint(SKColors.Red);
    }
                    }),
            };

            ScrollbarSeries = new ISeries[]
       {
            new LineSeries<SiteData>
            {
                Values = _values,
                GeometryStroke = null,
                GeometryFill = null,
                Fill=null,
                DataPadding=new LiveChartsCore.Drawing.LvcPoint(0,1),
                  Mapping = (s, p) =>
                 {
                    p.Coordinate=new LiveChartsCore.Kernel.Coordinate(p.Index,s.SiteValue);
                 },
            }
       };


            ScrollableAxes = new[] { new Axis()
            {
                Labeler=x=>{
                    if (x>0&&x<_values.Count)
    {
                        return _values[(int)x].EntDate.ToString("MM/dd");
    }
                    return "";
                },
                MinLimit=0,
                MaxLimit=50,
            } };

            ScrollableYAxes = new[] { new Axis()
            {

             CustomSeparators=new double[] {Math.Round(mean,2), Math.Round(mean + 1 * sigma, 2), Math.Round(mean - 1 * sigma, 2), Math.Round(mean + 2*sigma,2) , Math.Round(mean - 2 * sigma, 2), Math.Round(mean + 3 * sigma, 2), Math.Round(mean - 3 * sigma, 2), },
              
             //ShowSeparatorLines=false
            } };

            Thumbs = new[]
            {
            new RectangularSection
            {
                Fill = new SolidColorPaint(new SKColor(255, 205, 210, 100))
            }
        };
            InvisibleX = new[] { new Axis { IsVisible = false } };
            InvisibleY = new[] { new Axis { IsVisible = false } };

            // force the left margin to be 100 and the right margin 50 in both charts, this will
            // align the start and end point of the "draw margin",
            // no matter the size of the labels in the Y axis of both chart.
            var auto = LiveChartsCore.Measure.Margin.Auto;
            Margin = new Margin(100, auto, 50, auto);

            ChartUpdatedCommand = new RelayCommand<ChartCommandArgs>(args =>
            {
                var cartesianChart = (ICartesianChartView<SkiaSharpDrawingContext>)args.Chart;

                var x = cartesianChart.XAxes.First();

                // update the scroll bar thumb when the chart is updated (zoom/pan)
                // this will let the user know the current visible range
                var thumb = Thumbs[0];

                thumb.Xi = x.MinLimit;
                thumb.Xj = x.MaxLimit;
            });
            PointerDownCommand = new RelayCommand<PointerCommandArgs>(args =>
            {
                _isDown = true;
            });
            PointerUpCommand = new RelayCommand<PointerCommandArgs>(args =>
            {
                _isDown = false;
            });

            PointerMoveCommand = new RelayCommand<PointerCommandArgs>(args =>
            {
                if (!_isDown) return;

                var chart = (ICartesianChartView<SkiaSharpDrawingContext>)args.Chart;
                var positionInData = chart.ScalePixelsToData(args.PointerPosition);

                var thumb = Thumbs[0];
                var currentRange = thumb.Xj - thumb.Xi;

                // update the scroll bar thumb when the user is dragging the chart
                thumb.Xi = positionInData.X - currentRange / 2;
                thumb.Xj = positionInData.X + currentRange / 2;

                // update the chart visible range
                ScrollableAxes[0].MinLimit = thumb.Xi;
                ScrollableAxes[0].MaxLimit = thumb.Xj;
            });
        }

        public RelayCommand<ChartCommandArgs> ChartUpdatedCommand { get; set; }
        public RelayCommand<PointerCommandArgs> PointerDownCommand { get; set; }
        public RelayCommand<PointerCommandArgs> PointerMoveCommand { get; set; }
        public RelayCommand<PointerCommandArgs> PointerUpCommand { get; set; }

        private ObservableCollection<SiteData> MockData()
        {
            ObservableCollection<SiteData> _values = new ObservableCollection<SiteData>();
            for (int i = 0; i < 1500; i++)
            {
                SiteData siteData = new SiteData();
                siteData.SiteValue = random.Next(1800, 2000) / 1000d;
                int month = random.Next(9, 12);
                int day = random.Next(1, 31);
                int hour = random.Next(0, 24);
                int minute = random.Next(0, 60);
                int second = random.Next(0, 60);
                var date = new DateTime(2023, month, day, hour, minute, second);
                siteData.EntDate = date;
                _values.Add(siteData);
            }


            for (int i = 0; i < 10; i++)
            {
                _values[random.Next(0, 1500)].SiteValue = random.Next(1600, 2000) / 1000d;
            }
            for (int i = 0; i < 20; i++)
            {
                _values[random.Next(0, 1500)].SiteValue = random.Next(1800, 2300) / 1000d;
            }


            mean = _values.Select(x => x.SiteValue).Take(100).Mean();
            sigma = _values.Select(x => x.SiteValue).Take(100).PopulationStandardDeviation();
            _values = new ObservableCollection<SiteData>(_values.Skip(100));
            _values.ToList().ForEach(x => x.Sigma = Math.Round(Math.Abs((x.SiteValue - mean) / sigma), 3));
            _values = new ObservableCollection<SiteData>(_values.OrderBy(x => x.EntDate));
            return _values;
        }

        double mean;
        double sigma;
    }

    class SiteData
    {
        public double SiteValue { get; set; }
        public DateTime EntDate { get; set; }
        public double Sigma { get; set; }
    }
}
