using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoundCorrelate
{
    public interface ISpectrumSource
    {
        int SliceCount { get; }
        int SamplesPerSlice { get; }

        double MaxMagnitude { get; }
        double MinMagnitude { get; }
        
        double MagnitudeAt(int sliceNumber, int sampleNumber);
        string GetSampleNumberLabel(int sample);
    }
}
