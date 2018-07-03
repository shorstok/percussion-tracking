using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using MathNet.Numerics.IntegralTransforms;
using MathNet.Filtering;

namespace SoundCorrelate.MFCC
{
    public class Mfcc
    {
        public const int BlockLength = 2048;
        public double[] Frame;        //один фрейм
        public double[,] FrameMass;  //массив всех фреймов по BlockLength отсчетов или 128 (for 16khz) мс        
        public Complex[,] FrameMassFft;     //массив результатов FFT для всех фреймов

        readonly int[] _filterPoints = {6,18,31,46,63,82,103,127,154,184,218,
                              257,299,348,402,463,531,608,695,792,901,1023};//массив опорных точек для фильтрации спекрта фрейма

        readonly double[,] _h = new double[20, BlockLength/2];     //массив из 20-ти фильтров для каждого MFCC

        /// <summary>
        /// Функция для расчета MFCC для сигнала с частотой дискретизации 16кГц
        /// </summary>
        /// <param name="wavPcm">Массив значений амплитуд аудиосигнала</param>
        /// <returns>Массив из 20-ти MFCC</returns>
        public double[,] MFCC_20_calculation(double[] wavPcm)
        {
            int countFrames = (wavPcm.Length * 2 / BlockLength) + 1; //количество отрезков в сигнале

            // RMS_gate(wavPcm);          //применение noise gate
            Normalize(wavPcm); //нормализация
            FrameMass = SplitToFrames(wavPcm); //формирование массива фреймов
            ApplyHammingWindow(FrameMass); //окно Хэмминга для каждого отрезка
            FrameMassFft = CalculateFramesFFT(FrameMass); //FFT для каждого фрейма

            double[,] mfccMass = new double[countFrames, 20]; //массив наборов MFCC для каждого фрейма

            //***********   Расчет гребенчатых фильтров спектра:    *************
            for (int i = 0; i < 20; i++)
            {
                for (int j = 0; j < BlockLength / 2; j++)
                {
                    if (j < _filterPoints[i]) _h[i, j] = 0;
                    if ((_filterPoints[i] <= j) & (j <= _filterPoints[i + 1]))
                        _h[i, j] = ((double)(j - _filterPoints[i]) / (_filterPoints[i + 1] - _filterPoints[i]));
                    if ((_filterPoints[i + 1] <= j) & (j <= _filterPoints[i + 2]))
                        _h[i, j] = ((double)(_filterPoints[i + 2] - j) / (_filterPoints[i + 2] - _filterPoints[i + 1]));
                    if (j > _filterPoints[i + 2]) _h[i, j] = 0;
                }
            }

            for(int nframe = 0; nframe < countFrames; nframe++)
            {
                //**********    Применение фильтров и логарифмирование энергии спектра для каждого фрейма   ***********
                double[] s = new double[20];
                for (int i = 0; i < 20; i++)
                {
                    for (int j = 0; j < (BlockLength / 2); j++)
                        s[i] += Math.Pow(FrameMassFft[nframe, j].Magnitude, 2) * _h[i, j];

                    if (Math.Abs(s[i]) > float.Epsilon)
                        s[i] = Math.Log(s[i], Math.E);
                }

                //**********    DCT и массив MFCC для каждого фрейма на выходе     ***********
                for (int l = 0; l < 20; l++)
                    for (int i = 0; i < 20; i++) mfccMass[nframe, l] += s[i] * Math.Cos(Math.PI * l * (i * 0.5 / 20));
            }

            return mfccMass;
        }


        /// <summary>
        /// Функция для подавления шума по среднекравратичному уровню
        /// </summary>
        /// <param name="wavPcm">Массив значений амплитуд аудиосигнала</param>
        private void RMS_gate(double[] wavPcm)
        {
            int k = 0;
            double rms = 0;

            for (int j = 0; j < wavPcm.Length; j++)
            {
                if (k < 100)
                {
                    rms += Math.Pow((wavPcm[j]), 2);
                    k++;
                }
                else
                {
                    if (Math.Sqrt(rms / 100) < 0.005)
                        for (int i = j - 100; i <= j; i++) wavPcm[i] = 0;
                    k = 0; rms = 0;
                }
            }
        }

        /// <summary>
        /// Функция нормализации сигнала
        /// </summary>
        /// <param name="wavPcm">Массив значений амплитуд аудиосигнала</param>
        private void Normalize(double[] wavPcm)
        {
            double[] absWavBuf = new double[wavPcm.Length];
            for (int i = 0; i < wavPcm.Length; i++)
                if (wavPcm[i] < 0) absWavBuf[i] = -wavPcm[i];   //приводим все значения амплитуд к абсолютной величине 
                else absWavBuf[i] = wavPcm[i];                    //для определения максимального пика
            double max = absWavBuf.Max();
            double k = 1f / max;        //получаем коэффициент нормализации            

            for (int i = 0; i < wavPcm.Length; i++) //записываем нормализованные значения в исходный массив амплитуд

                wavPcm[i] = wavPcm[i] * k;
        }

        /// <summary>
        /// Функция для формирования двумерного массива отрезков сигнала длиной по 128мс.
        /// При этом начало каждого следующего отрезка делит предыдущий пополам
        /// </summary>
        /// <param name="wavPcm">Массив значений амплитуд аудиосигнала</param>
        private double[,] SplitToFrames(double[] wavPcm)
        {
            int countFrames = 0;
            int countSamp = 0;

            var frameMass1 = new double[wavPcm.Length * 2 / BlockLength + 1, BlockLength];
            for (int j = 0; j < wavPcm.Length; j++)
            {
                if (j >= (BlockLength/2))      //запись фреймов в массив
                {
                    countSamp++;
                    if (countSamp >= BlockLength+1)
                    {
                        countFrames += 2;
                        countSamp = 1;
                    }
                    frameMass1[countFrames, countSamp - 1] = wavPcm[j - (BlockLength/2)];
                    frameMass1[countFrames + 1, countSamp - 1] = wavPcm[j];
                }
            }
            return frameMass1;
        }


        /// <summary>
        /// Оконная функция Хэмминга
        /// </summary>
        /// <param name="frames">Двумерный массив отрезвов аудиосигнала</param>
        public static void ApplyHammingWindow(double[,] frames)
        {
            double w = 2.0 * Math.PI / BlockLength;

            for (int nframe = 0; nframe < frames.GetLength(0); nframe++)
                for (int nsample = 0; nsample < BlockLength; nsample++)
                    frames[nframe, nsample] = (0.54 - 0.46 * Math.Cos(w * nsample)) * frames[nframe, nsample];
        }


        /// <summary>
        /// Быстрое преобразование фурье для набора отрезков
        /// </summary>
        /// <param name="frames">Двумерный массив отрезвов аудиосигнала</param>
        /// <param name="wav_PCM">Массив значений амплитуд аудиосигнала</param>
        private Complex[,] CalculateFramesFFT(double[,] frames)
        {
            var frameMassComplex = new Complex[frames.GetLength(0), BlockLength]; //для хранения результатов FFT каждого фрейма в комплексном виде

            var fftFrame = new Complex[BlockLength];     //спектр одного фрейма

            for (int k = 0; k < frames.GetLength(0); k++)
            {
                for (int i = 0; i < BlockLength; i++)
                    fftFrame[i] = frames[k, i];

                Fourier.Forward(fftFrame, FourierOptions.Matlab);

                for (int i = 0; i < BlockLength; i++)
                    frameMassComplex[k, i] = fftFrame[i];
            }
            return frameMassComplex;
        }
    }

}
