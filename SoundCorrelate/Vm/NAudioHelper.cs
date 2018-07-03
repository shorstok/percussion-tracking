using System.Collections.Generic;
using System.Threading.Tasks;
using NAudio.Wave;

namespace SoundCorrelate.Vm
{
    public static class NAudioHelper
    {
        public static async Task<float[]> ReadAudioFile(string source)
        {
            float[] block = new float[1024];
            var result = new List<float>();

            using (var reader = new AudioFileReader(source))            
            {
                int nread = 0;

                do
                {
                    await Task.Run(() =>
                    {

                        nread = reader.Read(block, 0, block.Length);

                        for (int i = 0; i < nread; i++)
                            result.Add(block[i]);
                    });

                } while (nread > 0);
            }

            return result.ToArray();
        }
    }
}