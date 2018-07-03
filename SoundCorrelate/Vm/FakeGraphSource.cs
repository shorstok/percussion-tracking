using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using SoundCorrelate.Annotations;

namespace SoundCorrelate.Vm
{
    public class FakeGraphSource : ISpectrumSource, INotifyPropertyChanged
    {
        public int SliceCount => 100;
        public int SamplesPerSlice => 100;

        private int _updateCounter = 0;

        public double MagnitudeAt(int sliceNumber, int sampleNumber)
        {
            var x = (double) sliceNumber / SliceCount;
            var y = (double) sampleNumber / SamplesPerSlice;

            x = Math.Sin(x*Math.PI);
            y = Math.Sin(y * Math.PI);

            return Math.Sin(x * Math.PI * 2 + _updateCounter + 124) * 0.25 +
                   Math.Sin(y * Math.PI * 3 + _updateCounter + 1) * 0.25 + 0.5;
        }

        public string GetSampleNumberLabel(int sample)
        {
            return "";
        }

        public FakeGraphSource()
        {
            FakeUpdate();
        }

        private async void FakeUpdate()
        {
            for (int i = 0; i < 100; i++)
            {
                await Task.Delay(30);

                _updateCounter++;

                OnPropertyChanged(nameof(SamplesPerSlice));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public double MaxMagnitude => 1;
        public double MinMagnitude => 0;
    }
}