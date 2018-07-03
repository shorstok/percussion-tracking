using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoundCorrelate.MFCC
{
    public static class Helpers
    {
        public static IEnumerable<IEnumerable<double>> SplitToFrames(double[] source, int frameLen)
        {
            int x = 0;

            while (x < source.Length)
            {
                yield return source.Skip(x).Take(frameLen);

                x += frameLen / 2;
            }
        }

        public static IEnumerable<double> ApplyHammingWindow(IEnumerable<double> data, int windowSize)
        {
            double w = 2.0 * Math.PI / windowSize;

            int nsample = 0;

            foreach (var item in data.Take(windowSize))
            {
                yield return (0.54 - 0.46 * Math.Cos(w * nsample)) * item;
                nsample++;               
            }
            
        }

        public static void ApplyHammingWindow(double[] data)
        {
            double w = 2.0 * Math.PI / data.Length;

            for (int nsample = 0; nsample < data.Length; nsample++)
                data[nsample] = (0.54 - 0.46 * Math.Cos(w * nsample)) * data[nsample];
        }
    }
}
