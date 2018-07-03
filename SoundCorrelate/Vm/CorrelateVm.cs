using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using LiveCharts;
using LiveCharts.Wpf;
using SoundCorrelate.Annotations;

namespace SoundCorrelate.Vm
{
    public class CorrelateVm : INotifyPropertyChanged
    {
        private string _status;
        private SeriesCollection _chartSeries;
        public AudioSampleVm Reference { get; }
        public AudioSampleVm Impulse { get; }

        public SeriesCollection ChartSeries
        {
            get { return _chartSeries; }
            set
            {
                if (Equals(value, _chartSeries)) return;
                _chartSeries = value;
                OnPropertyChanged();
            }
        }

        public string Status
        {
            get { return _status; }
            set
            {
                if (value == _status) return;
                _status = value;
                OnPropertyChanged();
            }
        }

        public CorrelateVm(AudioSampleVm reference, AudioSampleVm impulse)
        {
            Reference = reference;
            Impulse = impulse;
            Status = "created";
            ChartSeries = new SeriesCollection();
        }

        public static int MaxSamplesInOutput = 1024;

        public async Task CalculateCorrelationAsync()
        {
            Status = "calculating...";
            ChartSeries.Clear();

            var data  = await Task.Run(Correlate);

            var series = new double[Math.Min(MaxSamplesInOutput, Impulse.SliceCount)];

            var skip = data.Length / series.Length - 1;

            if (skip < 0)
                skip = 0;

            for (int i = 0; i < series.Length; i++)
                series[i] = data[i * (skip + 1)];

            ChartSeries.Add(new StepLineSeries
            {
                PointGeometry = null,
                StrokeThickness = 1,
                
                Title = Path.GetFileName(Impulse.FileName),                
                Values = new ChartValues<double>(series)
            });
            
            Status = $"{Path.GetFileName(Impulse.FileName)} done";
        }

        private double EuclideanDistance(IEnumerable<double> one, IEnumerable<double> other)
        {
            double euclidean = 0;
            int steps = 0;

            using (var en1 = one.GetEnumerator())
            using (var en2 = other.GetEnumerator())
                while (en1.MoveNext() && en2.MoveNext())
                {
                    euclidean += (en1.Current - en2.Current) * (en1.Current - en2.Current);
                    steps++;
                }

            return Math.Sqrt(euclidean/steps);
        }

        private double Corr(IEnumerable<double> one, IEnumerable<double> other)
        {
            double euclidean = 0;

            using (var en1 = one.GetEnumerator())
            using (var en2 = other.GetEnumerator())
                while (en1.MoveNext() && en2.MoveNext())
                    euclidean += en1.Current * en2.Current;

            return euclidean;
        }

        public double GeneralCorrelation(double[] reference, double[] impulse, int impulseOffset)
        {
            var ravg = reference.Average();
            var iavg = impulse.Average();

            double sum = 0;

            for (int i = 0; i < impulse.Length; i++)
            {
                if(i + impulseOffset >= reference.Length)
                    continue;

                sum += (reference[i + impulseOffset] - ravg) * (impulse[i] - iavg);
            }

            var sumSqr1 = reference.Sum(x => Math.Pow(x - ravg, 2.0));
            var sumSqr2 = impulse.Sum(y => Math.Pow(y - iavg, 2.0));
            
            return sum / Math.Sqrt(sumSqr1 * sumSqr2); 
        }

        private async Task<double[]> Correlate()
        {
            var result = new double[Reference.SliceCount];
            var tenPercent = Reference.SliceCount / 10;
            
            for (int d = 0; d < Reference.SliceCount; d++)
            {
                if (d % tenPercent == 0)
                    Status = $"Calculating, {100 * d / Reference.Samples.Length}% done...";

                double r = 0;

                for (int i = 0; i < Impulse.SliceCount; i++)
                {
                    if(Reference.SliceCount <= i+d)
                        continue;

                    r+= Math.Pow(EuclideanDistance(Reference.SliceAt(d+i), Impulse.SliceAt(i)),2);
                }

                result[d] = -Math.Sqrt(r/ Impulse.SliceCount);
            }

/*            var min = result.Min();
            var max = result.Max();

            for (int i = 0; i < result.Length; i++)
                result[i] = (result[i] - min) / (max - min);*/

            return result;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}