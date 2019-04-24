using NAudio.Dmo;
using NAudio.Dsp;
using NAudio.Wave;
using NAudio.Wave.Compression;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CrossTalkServer
{
	class Resampler
	{
		// RESAMPLER OBJECTS
		BufferedWaveProvider provider;
		WdlResamplingSampleProvider resampler;
		WaveFormat inputFormat;
		WaveFormat outputFormat;

		//static MediaFoundationResampler resampler;
		float[] outFloat;

		/// <summary>
		/// BOTH FORMATS HAVE TO BE IEEE-FLOAT AND 2 CHANNELS
		/// </summary>
		/// <param name="input"></param>
		/// <param name="output"></param>
		public Resampler(WaveFormat input, WaveFormat output)
		{
			inputFormat = input;
			outputFormat = output;
			provider = new BufferedWaveProvider(input);
			provider.ReadFully = true;
			provider.DiscardOnBufferOverflow = true;
			resampler = new WdlResamplingSampleProvider(provider.ToSampleProvider(), outputFormat.SampleRate);
		}

		/// <summary>
		/// ONLY WORKS WITH FLOAT DATA, AND 2 CHANNELS
		/// </summary>
		/// <param name="input"></param>
		/// <param name="count"></param>
		/// <returns></returns>
		public byte[] inputResample(byte[] input, int count)
		{
			//provider = new RawSourceWaveStream(input, 0, count, inputFormat);
			provider.AddSamples(input, 0, input.Length);

			int outSamples = (int)Math.Floor(((double)outputFormat.SampleRate / (double)inputFormat.SampleRate) * count);

			outFloat = new float[outSamples];
			resampler.Read(outFloat, 0, outFloat.Length);
			return floats2bytes(outFloat);
		}

		private byte[] floats2bytes(float[] input)
		{
			byte[] output = new byte[input.Length * 4];
			Buffer.BlockCopy(input, 0, output, 0, output.Length);

			return output;
		}

		private float[] bytes2floats(byte[] input)
		{
			if (input.Length % 4 != 0)
			{
				throw new ArgumentException("Bytes2Floats input is not a multiple of 4, this will create trouble. Input was " + input.Length + " bytes.");
			}

			float[] output = new float[input.Length / 4];
			Buffer.BlockCopy(input, 0, output, 0, input.Length);

			return output;
		}
	}
}
