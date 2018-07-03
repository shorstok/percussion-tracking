using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using SoundCorrelate.Annotations;

namespace SoundCorrelate.Vm
{
    public partial class AudioSampleVm : INotifyPropertyChanged, ISpectrumSource
    {
        private string _fileName;
        private string _state;
        private double[] _samples;
        private bool _isReady;
        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsReady
        {
            get { return _isReady; }
            set
            {
                if (value == _isReady) return;
                _isReady = value;
                OnPropertyChanged();
            }
        }

        public string State
        {
            get { return _state; }
            set
            {
                if (value == _state) return;
                _state = value;
                OnPropertyChanged();
            }
        }

        public string FileName
        {
            get { return _fileName; }
            private set
            {
                if (value == _fileName) return;
                _fileName = value;
                OnPropertyChanged();
            }
        }

        public double[] Samples
        {
            get { return _samples; }
            set
            {
                if (Equals(value, _samples)) return;
                _samples = value;
                OnPropertyChanged();
            }
        }

        public AudioSampleVm(string source)
        {
            FileName = source;
            State = "dummy";
            IsReady = false;

            LoadSelf();
        }

        public async void LoadSelf()
        {
            try
            {
                State = "loading...";

                await Task.Delay(1);

                var data  = await NAudioHelper.ReadAudioFile(FileName);

                Samples = new double[data.Length];

                for (int i = 0; i < Samples.Length; i++)
                    Samples[i] = data[i];

                State = "Calculating spectral info";

                CalculateSpecturm();
                CalculateCentroids();

                State = "OK";
                IsReady = true;
            }
            catch (Exception e)
            {
                State = $"!Failed: {e.Message}";
            }

            CommandManager.InvalidateRequerySuggested();
        }
        
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


    }
}