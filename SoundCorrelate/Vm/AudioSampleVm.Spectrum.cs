using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Numerics;
using MathNet.Numerics.IntegralTransforms;
using SoundCorrelate.MFCC;

namespace SoundCorrelate.Vm
{
    public partial class AudioSampleVm : INotifyPropertyChanged, ISpectrumSource
    {
        public int SliceCount { get; private set; }
        public int SamplesPerSlice { get; private set; }
        public double MaxMagnitude { get; private set; }
        public double MinMagnitude { get; private set; }

        public double[] Centroids { get; set; }

        private double[,] _mfccData = null;

        private double samplerate = 44100;
        public int SourceBlockLength = 400;

        private string _maxFreq;
        public string MaxFreq
        {
            get { return _maxFreq; }
            set
            {
                if (value == _maxFreq) return;
                _maxFreq = value;
                OnPropertyChanged();
            }
        }

        public IEnumerable<double> SliceAt(int nSlice)
        {
            for (int i = 0; i < _mfccData.GetLength(1); i++)
                yield return _mfccData[nSlice, i];
        }

        public IEnumerable<double> SampleAt(int sample)
        {
            for (int i = 0; i < _mfccData.GetLength(0); i++)
                yield return _mfccData[i, sample];
        }

        
        public double MagnitudeAt(int sliceNumber, int sampleNumber)
        {
            return _mfccData[sliceNumber, sampleNumber];
        }

        public string GetSampleNumberLabel(int sample)
        {
            return (sample* samplerate / SourceBlockLength).ToString(CultureInfo.InvariantCulture);
        }

        private void CalculateCentroids()
        {
            var centroids = new List<double>();
            var minFq = 10.0;
            var centroidSlicerBlock = 2048;

            foreach (var frame in Helpers.SplitToFrames(Samples, centroidSlicerBlock))
            {
                var fftFrame = Helpers.ApplyHammingWindow(frame, centroidSlicerBlock).Select(s => new Complex(s,0)).ToArray();

                Fourier.Forward(fftFrame, FourierOptions.Matlab);

                double centroid = 0;
                double magsum = 0;

                var ox =
                    fftFrame.Take(fftFrame.Length / 2)
                        .Select((s, i) => new {magnitude = s.Magnitude, idx = i})
                        .OrderByDescending(s => s.magnitude)
                        .ToArray();

                var totalenergy = ox.Sum(s => s.magnitude * s.magnitude);

                var harmonicEnergy = new List<Tuple<double,int>>();

                double energy = 0;
                int index=0;

                while (energy < totalenergy*0.6)
                {
                    harmonicEnergy.Add(new Tuple<double, int>(ox[index].magnitude,ox[index].idx));
                    energy += ox[index].magnitude * ox[index].magnitude;
                    index++;
                }
                
                foreach (var item in harmonicEnergy)
                {
                    var mag = item.Item1;

                    var fq = item.Item2 * samplerate / fftFrame.Length;

                    magsum += mag;
                    
                    centroid += fq * mag;
                }

                if(magsum > double.Epsilon)
                    centroid /= magsum;

                centroids.Add(centroid);
            }

            Centroids = centroids.ToArray();

            var cids = Centroids.OrderByDescending(s => s).ToArray();

            var median = cids[cids.Length / 2];
            
            Console.WriteLine($@"{FileName}: centroid {median} Hz");
        }

        private void CalculateSpecturm()
        {
            SliceCount = Samples.Length / SourceBlockLength;
            SamplesPerSlice = SourceBlockLength / 2 + 1;

            _mfccData = new double[SliceCount,SamplesPerSlice];

            MaxMagnitude = double.NegativeInfinity;
            MinMagnitude = double.PositiveInfinity;

            var mfcc = new MFCC.Mfcc();
            
            _mfccData = mfcc.MFCC_20_calculation(Samples);

            SliceCount = _mfccData.GetLength(0);
            SamplesPerSlice = _mfccData.GetLength(1);

            for (int nslice = 0; nslice < SliceCount; nslice++)
            {
                for (int c = 0; c < SamplesPerSlice; c++)
                {
                    MaxMagnitude = Math.Max(MaxMagnitude, _mfccData[nslice,c]);
                    MinMagnitude = Math.Min(MinMagnitude, _mfccData[nslice,c]);
                }
            }


        }


    }
}