using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace SoundCorrelate
{
    /// <summary>
    /// Interaction logic for SpectrumGraph.xaml
    /// </summary>
    public partial class SpectrumGraph : UserControl
    {
        public static readonly DependencyProperty SpectrumSourceProperty = DependencyProperty.Register(
            "SpectrumSource", typeof(ISpectrumSource), typeof(SpectrumGraph), new PropertyMetadata(default(ISpectrumSource), SpecturmSourceChanged));

        private static void SpecturmSourceChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var spectrumGraph = ((SpectrumGraph) dependencyObject);

            spectrumGraph.RecalculateSpectrum();

            var inpc = dependencyPropertyChangedEventArgs.NewValue as INotifyPropertyChanged;

            if (inpc != null)
                spectrumGraph.SubscribeToUpdates(inpc);
        }

        private void SubscribeToUpdates(INotifyPropertyChanged inpc)
        {
            PropertyChangedEventManager.AddHandler(inpc, (sender, args) => RecalculateSpectrum(), String.Empty);
        }

        public ISpectrumSource SpectrumSource
        {
            get { return (ISpectrumSource) GetValue(SpectrumSourceProperty); }
            set { SetValue(SpectrumSourceProperty, value); }
        }

        public static readonly DependencyProperty MaxEnergyColorProperty = DependencyProperty.Register(
            "MaxEnergyColor", typeof(Color), typeof(SpectrumGraph), new PropertyMetadata(Colors.Orange));

        public Color MaxEnergyColor
        {
            get { return (Color) GetValue(MaxEnergyColorProperty); }
            set { SetValue(MaxEnergyColorProperty, value); }
        }

        public static readonly DependencyProperty MinEnergyColorProperty = DependencyProperty.Register(
            "MinEnergyColor", typeof(Color), typeof(SpectrumGraph), new PropertyMetadata(Colors.Transparent));

        public Color MinEnergyColor
        {
            get { return (Color) GetValue(MinEnergyColorProperty); }
            set { SetValue(MinEnergyColorProperty, value); }
        }

        public static readonly DependencyProperty InvalidEnergyColorProperty = DependencyProperty.Register(
            "InvalidEnergyColor", typeof(Color), typeof(SpectrumGraph), new PropertyMetadata(Colors.DeepPink));

        public Color InvalidEnergyColor
        {
            get { return (Color) GetValue(InvalidEnergyColorProperty); }
            set { SetValue(InvalidEnergyColorProperty, value); }
        }

        public SpectrumGraph()
        {
            InitializeComponent();
            Loaded += SpectrumGraph_Loaded;
        }

        private void SpectrumGraph_Loaded(object sender, RoutedEventArgs e) => RecalculateSpectrum();

        private Color GetColorForValue(double value)
        {
            if (value < 0 || value > 1)
                return InvalidEnergyColor;

            return Color.FromArgb(
                (byte)(MaxEnergyColor.A * value + (1 - value) * MinEnergyColor.A),
                (byte)(MaxEnergyColor.R * value + (1 - value) * MinEnergyColor.R),
                (byte)(MaxEnergyColor.G * value + (1 - value) * MinEnergyColor.G),
                (byte)(MaxEnergyColor.B * value + (1 - value) * MinEnergyColor.B));
        }

        private async void RecalculateSpectrum()
        {
            if(null == SpectrumSource)
                return;

            await Task.Delay(1);

            var bitmap = new WriteableBitmap(SpectrumSource.SliceCount, SpectrumSource.SamplesPerSlice,96,96,PixelFormats.Bgra32, null);

            byte[] data = new byte[SpectrumSource.SliceCount * SpectrumSource.SamplesPerSlice*4];

            for (int xc = 0; xc < SpectrumSource.SliceCount; xc++)
            {
                for (int yc = 0; yc < SpectrumSource.SamplesPerSlice; yc++)
                {
                    var magNorm = (SpectrumSource.MagnitudeAt(xc, SpectrumSource.SamplesPerSlice - 1 - yc) -
                                   SpectrumSource.MinMagnitude) /
                                  (SpectrumSource.MaxMagnitude - SpectrumSource.MinMagnitude);

                    /*magNorm = Math.Log(magNorm,500)+1;
                    magNorm = Math.Max(0, magNorm);*/

                    var col = GetColorForValue(magNorm);

                    data[(yc * SpectrumSource.SliceCount + xc) * 4] = col.B;//b
                    data[(yc * SpectrumSource.SliceCount + xc) * 4+1] = col.G;//g
                    data[(yc * SpectrumSource.SliceCount + xc) * 4 + 2] = col.R;
                    data[(yc * SpectrumSource.SliceCount + xc) * 4+3] = col.A;//a
                }
            }

            bitmap.WritePixels(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight), data, bitmap.BackBufferStride, 0);

            Spectrum.Source = bitmap;
            Spectrum.InvalidateVisual();
        }
    }
}
