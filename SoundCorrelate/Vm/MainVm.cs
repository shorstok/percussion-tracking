using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using SoundCorrelate.Annotations;

namespace SoundCorrelate.Vm
{
    public class MainVm : INotifyPropertyChanged
    {
        private string _mainMusicPieceMessage;
        private AudioSampleVm _mainSample;
        private bool _autoloadImpulses = true;
        public event PropertyChangedEventHandler PropertyChanged;

        public ICommand CalculateCorrelations { get; }
        public ObservableCollection<AudioSampleVm> Samples { get; } = new ObservableCollection<AudioSampleVm>();
        public ObservableCollection<CorrelateVm> Correlations { get; } = new ObservableCollection<CorrelateVm>();

        public FakeGraphSource FakeGraph { get; } = new FakeGraphSource();

        public string MainMusicPieceMessage
        {
            get { return _mainMusicPieceMessage; }
            set
            {
                if (value == _mainMusicPieceMessage) return;
                _mainMusicPieceMessage = value;
                OnPropertyChanged();
            }
        }

        public AudioSampleVm MainSample
        {
            get { return _mainSample; }
            set
            {
                if (Equals(value, _mainSample)) return;
                _mainSample = value;
                OnPropertyChanged();
            }
        }

        public bool CanCalculateCorrelations
            => MainSample?.IsReady == true && Samples.Any() && Samples.All(s => s.IsReady);

        public bool AutoloadImpulses
        {
            get { return _autoloadImpulses; }
            set
            {
                if (value == _autoloadImpulses) return;
                _autoloadImpulses = value;
                OnPropertyChanged();
            }
        }

        public Action OnBeforeClose { get; set; }

        public MainVm()
        {

            OnBeforeClose = () => { };
            
            CalculateCorrelations = new AwaitableDelegateCommand<object>(DoCalcCorrelations,
                (o) => CanCalculateCorrelations);

            if (AutoloadImpulses)
            {
                LoadDataFromCurrentFolder();
            }
        }

        private void LoadDataFromCurrentFolder()
        {
            var sources = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.wav").
                Select(fn => new FileInfo(fn)).
                OrderByDescending(fi => fi.Length).ToArray();

            if (sources.Length < 2)
                return;

            MainSample = new AudioSampleVm(sources[0].FullName);

            for (int i = 1; i < sources.Length; i++)
                Samples.Add(new AudioSampleVm(sources[i].FullName));
        }

        private async Task DoCalcCorrelations(object o)
        {
            Correlations.Clear();

            foreach (var sample in Samples)
                Correlations.Add(new CorrelateVm(MainSample, sample));

            await Task.WhenAll(Correlations.Select(c => c.CalculateCorrelationAsync()));
        }


        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void LoadMainPiece(string sourceFile)
        {
            MainSample = new AudioSampleVm(sourceFile);
            MainMusicPieceMessage = MainSample.State;
        }
    }
}